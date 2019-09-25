using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class newSence: MonoBehaviour
{
    private bool isSwitch;
    public void LoadNewScene()
    {
        isSwitch = !isSwitch;
        if (isSwitch)
        {
            Globe.nextSceneName = "MainScene";
            SceneManager.LoadScene("LoadingScene");
        }
        //保存需要加载的目标场景


    }

    void Start()
    {
        isSwitch = false;
    }

    void Update()
    {
    }

    public void exit()
    {
        Application.Quit();
    }
}
