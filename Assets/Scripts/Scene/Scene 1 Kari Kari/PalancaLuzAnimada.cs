using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PalancaLuzAnimada : MonoBehaviour
{
    [Header("Configuración")]
    public List<LuzSagrada> lucesObjetivo = new List<LuzSagrada>();

    [Header("Animación de la palanca")]
    public float duracionAnim = 0.4f;
    public float rotacionBajada = 45f;

    [Header("Apagado automático")]
    public AudioSource audioGrito;
    public AudioClip clipGrito;

    public bool encendida = false;
    private bool enMovimiento = false;
    private Quaternion rotacionInicial;
    private Quaternion rotacionFinal;

    public bool EstadoEncendido => encendida;

    void Start()
    {
        // Forzar apagado completo antes de configurar nada
        encendida = false;
        enMovimiento = false;

        // Guardar la rotación inicial real del objeto (tal como está en el editor)
        rotacionInicial = transform.localRotation;
        rotacionFinal = Quaternion.Euler(transform.localEulerAngles + new Vector3(rotacionBajada, 0, 0));

        // Asegurar posición visual inicial (palanca arriba)
        transform.localRotation = rotacionInicial;

        // Desactivar luces y vincular eventos
        foreach (var luz in lucesObjetivo)
        {
            if (luz != null)
            {
                luz.Desactivar();
                luz.OnEnemyTouchLight += (l) => IniciarApagado(l);
            }
        }

        // Cortar cualquier corrutina previa que pueda rotarla
        StopAllCoroutines();
    }



    public void Activar()
    {
        if (enMovimiento) return;

        bool algunaDisponible = false;
        foreach (var luz in lucesObjetivo)
        {
            if (luz != null && !luz.estaTocada)
            {
                algunaDisponible = true;
                break;
            }
        }
        if (!algunaDisponible) return;

        encendida = !encendida;

        foreach (var luz in lucesObjetivo)
        {
            if (luz == null) continue;
            if (encendida) luz.Activar();
            else luz.Desactivar();
        }

        StopAllCoroutines();
        StartCoroutine(RotarPalanca(encendida));
    }

    public void ForzarEncendido()
    {
        encendida = true;

        foreach (var luz in lucesObjetivo)
        {
            if (luz != null)
                luz.Activar();
        }

        StopAllCoroutines();
        StartCoroutine(RotarPalanca(true));
    }

    private void IniciarApagado(LuzSagrada luz)
    {
        if (!encendida) return;
        StartCoroutine(ApagarDespuesDeTiempo(luz));
    }

    IEnumerator ApagarDespuesDeTiempo(LuzSagrada luz)
    {
        yield return new WaitForSeconds(3f);

        if (audioGrito && clipGrito)
            audioGrito.PlayOneShot(clipGrito);

        if (luz != null)
            luz.Desactivar();

        // Revisar si queda alguna luz encendida
        bool algunaEncendida = false;
        foreach (var l in lucesObjetivo)
        {
            if (l != null && l.encendida)
            {
                algunaEncendida = true;
                break;
            }
        }

        if (!algunaEncendida)
        {
            encendida = false;
            StopAllCoroutines();
            StartCoroutine(RotarPalanca(false));
        }
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
