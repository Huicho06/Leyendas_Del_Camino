using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dissolve : MonoBehaviour
{
    [SerializeField] private float _dissolveTime = 0.75f;

    private SpriteRenderer[] _spriteRenderers;
    private Material[] _materials;

    // IDs de propiedades del shader
    private readonly int _dissolveAmount = Shader.PropertyToID("_DissolveAmount");
    private readonly int _verticalDissolveAmount = Shader.PropertyToID("_VerticalDissolve");

    private void Start()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        _materials = new Material[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            // Usa la instancia del material del renderer
            _materials[i] = _spriteRenderers[i].material;
        }
    }

    /// <summary>
    /// Disuelve de 0 -> 1. 
    /// useDissolve controla _DissolveAmount; useVertical controla _VerticalDissolve.
    /// </summary>
    private IEnumerator Vanish(bool useDissolve, bool useVertical)
    {
        float elapsedTime = 0f;

        while (elapsedTime < _dissolveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _dissolveTime);

            // 0 -> 1 durante Vanish
            float lerpedDissolve = Mathf.Lerp(0f, 1f, t);
            float lerpedVerticalDissolve = Mathf.Lerp(0f, 1f, t);

            for (int i = 0; i < _materials.Length; i++)
            {
                if (useDissolve)
                    _materials[i].SetFloat(_dissolveAmount, lerpedDissolve);

                if (useVertical)
                    _materials[i].SetFloat(_verticalDissolveAmount, lerpedVerticalDissolve);
            }

            yield return null;
        }

        // Fuerza valor final exacto
        for (int i = 0; i < _materials.Length; i++)
        {
            if (useDissolve)
                _materials[i].SetFloat(_dissolveAmount, 1f);
            if (useVertical)
                _materials[i].SetFloat(_verticalDissolveAmount, 1f);
        }
    }

    /// <summary>
    /// Aparece de 1 -> 0.
    /// </summary>
    private IEnumerator Appear(bool useDissolve, bool useVertical)
    {
        float elapsedTime = 0f;

        while (elapsedTime < _dissolveTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _dissolveTime);

            // 1 -> 0 durante Appear
            float lerpedDissolve = Mathf.Lerp(1f, 0f, t);
            float lerpedVerticalDissolve = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < _materials.Length; i++)
            {
                if (useDissolve)
                    _materials[i].SetFloat(_dissolveAmount, lerpedDissolve);

                if (useVertical)
                    _materials[i].SetFloat(_verticalDissolveAmount, lerpedVerticalDissolve);
            }

            yield return null;
        }

        // Fuerza valor final exacto
        for (int i = 0; i < _materials.Length; i++)
        {
            if (useDissolve)
                _materials[i].SetFloat(_dissolveAmount, 0f);
            if (useVertical)
                _materials[i].SetFloat(_verticalDissolveAmount, 0f);
        }
    }

    // Métodos públicos para llamar desde otros scripts, botones o animaciones
    public void DoVanish(bool useDissolve = true, bool useVertical = false)
    {
        StopAllCoroutines();
        StartCoroutine(Vanish(useDissolve, useVertical));
    }

    public void DoAppear(bool useDissolve = true, bool useVertical = false)
    {
        StopAllCoroutines();
        StartCoroutine(Appear(useDissolve, useVertical));
    }
}
