using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NoMirarAtrasController : MonoBehaviour
{
    [Header("Configuración de cámara")]
    public Transform playerCamera;             // Cámara del jugador
    public float maxViewAngle = 140f;          // Ángulo máximo antes de “mirar atrás”

    [Header("Tentación y audio 3D")]
    public AudioSource audioSource;
    public AudioClip[] susurros;               // Lista de clips
    public float intervaloTentacion = 20f;     // Cada 20s activa modo
    public float distanciaDetras = 2.5f;       // Distancia detrás de la cámara
    public float duracionTentacion = 7f;       // DURACIÓN del efecto

    [Header("Barra de tentación")]
    public Slider barraTentacion;
    public Image barraFill;                    // Image del Fill del slider
    public float velocidadIncremento = 0.3f;
    public float velocidadDescenso = 0.15f;
    public Color colorNormal = Color.white;
    public Color colorPeligro = Color.red;
    public float intensidadTemblor = 6f;       // Vibración de la barra
    public float velocidadTemblor = 35f;

    [Header("Debug")]
    public bool debugLogs = false;

    // Estado interno
    private bool enModoTentacion = false;
    private Quaternion rotacionReferencia;      // <-- captura al ACTIVAR tentación
    private RectTransform barraRect;
    private Vector3 barraPosOriginal;
    private Coroutine rutinaTentacion;          // para no solapar
    private Coroutine rutinaFin;                // para cortar a los 7s

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main.transform;

        barraTentacion.gameObject.SetActive(false);

        // Audio 3D
        if (audioSource)
        {
            audioSource.spatialBlend = 1f;                 // 3D
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = 10f;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.dopplerLevel = 0f;
        }

        barraRect = barraTentacion.GetComponent<RectTransform>();
        barraPosOriginal = barraRect.anchoredPosition;

        rutinaTentacion = StartCoroutine(TentacionLoop());
    }

    IEnumerator TentacionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervaloTentacion);

            // No dispares una nueva si aún está activa
            if (!enModoTentacion)
                ActivarTentacion();
        }
    }

    void ActivarTentacion()
    {
        if (debugLogs) Debug.Log("Tentación activada");

        enModoTentacion = true;

        // Captura la rotación de la cámara JUSTO AHORA
        rotacionReferencia = playerCamera.rotation;        // <-- FIX CLAVE

        // Coloca el audio exactamente detrás en este instante
        Vector3 posDetras = playerCamera.position - playerCamera.forward * distanciaDetras;
        audioSource.transform.position = posDetras;

        // Reproduce clip aleatorio
        if (susurros.Length > 0 && audioSource)
        {
            audioSource.clip = susurros[Random.Range(0, susurros.Length)];
            audioSource.Play();
        }

        // Prepara barra
        barraTentacion.value = 0f;                         // reset
        barraTentacion.gameObject.SetActive(true);

        // Corta automáticamente a los X segundos
        if (rutinaFin != null) StopCoroutine(rutinaFin);
        rutinaFin = StartCoroutine(FinTentacionTrasTiempo(duracionTentacion));
    }

    IEnumerator FinTentacionTrasTiempo(float duracion)
    {
        yield return new WaitForSeconds(duracion);
        if (enModoTentacion) TerminarTentacion();
    }

    void TerminarTentacion()
    {
        enModoTentacion = false;
        barraTentacion.gameObject.SetActive(false);
        barraRect.anchoredPosition = barraPosOriginal;

        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();

        if (debugLogs) Debug.Log("Tentación finalizada");
    }

    void Update()
    {
        if (!enModoTentacion) return;

        // Mantener el susurro SIEMPRE detrás mientras dura
        if (audioSource && audioSource.isPlaying)
        {
            Vector3 posDetras = playerCamera.position - playerCamera.forward * distanciaDetras;
            audioSource.transform.position = posDetras;
        }

        // Ángulo respecto a la rotación CAPTURADA al activar la tentación
        float angulo = Quaternion.Angle(rotacionReferencia, playerCamera.rotation);

        // Control de barra
        if (angulo > 30f && angulo < maxViewAngle)
            barraTentacion.value += velocidadIncremento * Time.deltaTime;
        else
            barraTentacion.value -= velocidadDescenso * Time.deltaTime;

        barraTentacion.value = Mathf.Clamp01(barraTentacion.value);

        // Color + vibración
        barraFill.color = Color.Lerp(colorNormal, colorPeligro, barraTentacion.value);
        if (barraTentacion.value > 0.6f)
        {
            float offset = Mathf.Sin(Time.time * velocidadTemblor) * intensidadTemblor;
            barraRect.anchoredPosition = barraPosOriginal + new Vector3(offset, 0f, 0f);
        }
        else
        {
            barraRect.anchoredPosition = barraPosOriginal;
        }

        // Fallo por mirar atrás o llenar barra
        if (angulo >= maxViewAngle || barraTentacion.value >= 1f)
            ReiniciarNivel();

        // Si el audio terminó antes y la barra ya bajó, cierra el efecto
        if (!audioSource.isPlaying && barraTentacion.value <= 0f)
            TerminarTentacion();
    }

    void ReiniciarNivel()
    {
        if (debugLogs) Debug.Log("¡Miró atrás! Reiniciando nivel...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
