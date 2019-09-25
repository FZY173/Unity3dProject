using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class report : MonoBehaviour
{
    public GameObject BookControl;
    public GameObject xchart;
    public GameObject TextAccount;

    public Text[] Text_1;
    public Text[] Text_2;
    public Text[] Text_3;
    double[] force =new double[3];
    double[] Acceleration = new double[3];

    private int isShow = 0;
    public bool defineBool = false;
    public bool isCurrent2 = true;
    public bool isCurrent4 = true;

    public bool isfillData = false;


    void Start()
    {

        //  Debug.Log(position);
        //   calculate();
        xchart.SetActive(false);
        BookControl.SetActive(defineBool);
        TextAccount.SetActive(false);
    }

    private void Update()
    {
        Debug.Log(force[0] == null);
        if (defineBool)
        {
            isShow = FindObjectOfType<Book>().currentPage;
            if (isShow == 2 && isCurrent2)
            {
                isCurrent2 = false;
                TextAccount.SetActive(true);
            }

            if (isShow != 2)
            {
                isCurrent2 = true;
            }

            if (isShow == 4 && isCurrent4)
            {
                isCurrent4 = false;
                xchart.SetActive(true);

                if (isfillData)
                {
                    FindObjectOfType<controlData>().setData(force, Acceleration);              
                }
            }

            if (isShow != 4)
            {
                isCurrent4 = true;
            }
        }
    }

    public void fillingData() {
        isfillData = true;
        switch (Define.currtentData)
        {
            case 0:
                Text_1[0].text =(FindObjectOfType<famarControl>().Mass / 100f).ToString();
                Text_1[1].text = FindObjectOfType<Move>().speedFirst.ToString();
                Text_1[2].text = FindObjectOfType<Move>().speedSecont.ToString();
                break;
            case 1:
                Text_2[0].text = (FindObjectOfType<famarControl>().Mass / 100f).ToString();
                Text_2[1].text = FindObjectOfType<Move>().speedFirst.ToString();
                Text_2[2].text = FindObjectOfType<Move>().speedSecont.ToString();
                break;
            case 2:
                Text_3[0].text = (FindObjectOfType<famarControl>().Mass / 100f).ToString();
                Text_3[1].text = FindObjectOfType<Move>().speedFirst.ToString();
                Text_3[2].text = FindObjectOfType<Move>().speedSecont.ToString();
                break;
        }

      
        force[Define.currtentData] = FindObjectOfType<startReset>().force;
        Acceleration[Define.currtentData] = FindObjectOfType<Move>().calculate();
        Define.currtentData++;

        if (Define.currtentData > 2)
        {
            Define.currtentData = 0;
        }



}


    public void HideText_charts()
    {
        xchart.SetActive(false);
        TextAccount.SetActive(false);
    }

    public void HideBook()
    {
        defineBool = false;
        BookControl.SetActive(false);
    }




}
