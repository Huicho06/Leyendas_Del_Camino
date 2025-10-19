using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PalancaLuzAnimada : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Arrastra aquí todas las luces o LuzSagrada que debe activar esta palanca")]
    public List<LuzSagrada> lucesObjetivo = new List<LuzSagrada>();

    [Header("Animación de la palanca")]
    public float duracionAnim = 0.4f;    // tiempo de rotación
    public float rotacionBajada = 45f;   // grados de bajada

    [Header("Apagado automático")]
    public bool autoApagar = true;       // permite que se apague sola
    public float tiempoAutoApagado = 3f; // segundos antes de apagarse
    public AudioSource audioGrito;       // arrastra aquí el sonido del enemigo (AudioSource)
    public AudioClip clipGrito;          // clip del grito

    private bool encendida = false;
    private bool enMovimiento = false;
    private Quaternion rotacionInicial;
    private Quaternion rotacionFinal;

    public bool EstadoEncendido => encendida;

    void Start()
    {
        rotacionInicial = transform.localRotation;
        rotacionFinal = Quaternion.Euler(transform.localEulerAngles + new Vector3(rotacionBajada, 0, 0));
    }

    public void Activar()
    {
        if (enMovimiento) return;
        encendida = !encendida;

        foreach (var luz in lucesObjetivo)
        {
            if (luz == null) continue;
            if (encendida) luz.Activar();
            else luz.Desactivar();
        }

        StopAllCoroutines();
        StartCoroutine(RotarPalanca(encendida));

        if (encendida && autoApagar)
            StartCoroutine(ApagarDespuesDeTiempo());
    }

    IEnumerator ApagarDespuesDeTiempo()
    {
        yield return new WaitForSeconds(tiempoAutoApagado);

        // reproduce grito
        if (audioGrito && clipGrito)
        {
            audioGrito.PlayOneShot(clipGrito);
        }

        // apaga luces
        encendida = false;
        foreach (var luz in lucesObjetivo)
        {
            if (luz == null) continue;
            luz.Desactivar();
        }

        // devuelve la palanca a su posición original
        StopAllCoroutines();
        StartCoroutine(RotarPalanca(false));
    }

    IEnumerator RotarPalanca(bool haciaAbajo)
    {
        enMovimiento = true;
        float t = 0f;
        Quaternion inicio = transform.localRotation;
        Quaternion fin = haciaAbajo ? rotacionFinal : rotacionInicial;

        while (t < duracionAnim)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(inicio, fin, t / duracionAnim);
            yield return null;
        }

        transform.localRotation = fin;
        enMovimiento = false;
    }
}
