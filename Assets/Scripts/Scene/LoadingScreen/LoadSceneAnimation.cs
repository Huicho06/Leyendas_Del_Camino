
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class LoadSceneAnimation : MonoBehaviour
{
    [SerializeField] private Image _logoLoadig;
    [SerializeField] private TMP_Text _txtLoading;
    [SerializeField] private Image loadingImage;
    [SerializeField] private Sprite[] _loadingImages; // asignas manualmente las imágenes aquí

    void Start()
    {
        if (_loadingImages.Length > 0)
        {
            int randomIndex = Random.Range(0, _loadingImages.Length);
            loadingImage.sprite = _loadingImages[randomIndex];
        }
        else
        {
            Debug.LogWarning("No se asignaron imágenes en el inspector.");
        }
    }
    private void OnEnable()
    {
        StartCoroutine(TextLoadingDots());
    }
    private void FixedUpdate()
    {
        RotationImage();
    }
    private void RotationImage()
    {
        Vector3 rotation = new Vector3(0, 0, -10);
        _logoLoadig.transform.Rotate(rotation * 10 * Time.fixedDeltaTime);
    }
    private IEnumerator TextLoadingDots()
    {
        _txtLoading.text = "Loading";
        yield return new WaitForSeconds(0.5f);

        _txtLoading.text = "Loading.";
        yield return new WaitForSeconds(0.5f);

        _txtLoading.text = "Loading..";
        yield return new WaitForSeconds(0.5f);

        _txtLoading.text = "Loading...";
        yield return new WaitForSeconds(0.5f);

        StartCoroutine(TextLoadingDots());
    }
}
