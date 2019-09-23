using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class startSence : MonoBehaviour
{
    public GameObject settingUI;
    public GameObject selectUI;

    void Start()
    {
        settingUI.SetActive(false);
        selectUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void select() {
        selectUI.SetActive(true);
    }

    public void sit()
    {
        settingUI.SetActive(true);
    }

    public void  exit()
    {
        Application.Quit();
    }

    public void save() {
        settingUI.SetActive(false);
    }

    public void returns()
    {
        selectUI.SetActive(false);
    }


}
