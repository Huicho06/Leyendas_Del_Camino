using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoaderScene : MonoBehaviour
{
    public static LoaderScene Instance;
    private void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("LoaderScene");
        if (objs.Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
    }
    public void LoadSceneString(string nameScene)
    {
        SceneManager.LoadScene(ConstantGames.SCENELOADINGSCREEN);
        StartCoroutine(LoadSceneAsync(nameScene));
    }
    private IEnumerator LoadSceneAsync(string nameScene)
    {
        yield return new WaitForSeconds(3f);
        AsyncOperation operation = SceneManager.LoadSceneAsync(nameScene);

        yield return new WaitUntil(() => operation.progress <= 0.9);
    }
}
