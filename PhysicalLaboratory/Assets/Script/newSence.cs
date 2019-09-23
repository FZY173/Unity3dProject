using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class newSence: MonoBehaviour
{
    public Button switch_button;
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
        switch_button.onClick.AddListener(LoadNewScene);
    }
}
