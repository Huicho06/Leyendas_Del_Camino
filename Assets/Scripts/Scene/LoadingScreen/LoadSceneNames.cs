using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadSceneNames : MonoBehaviour
{
    public void SceneBeni()
    {
        LoaderScene.Instance.LoadSceneString(ConstantGames.BENI);
    }
    public void SceneLaPaz()
    {
        LoaderScene.Instance.LoadSceneString(ConstantGames.LAPAZ);
    }
    public void SceneScene1()
    {
        LoaderScene.Instance.LoadSceneString(ConstantGames.SCENE1);
    }
    public void SceneTransision()
    {
        LoaderScene.Instance.LoadSceneString(ConstantGames.TRNASICION);
    }
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
