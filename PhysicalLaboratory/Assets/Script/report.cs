using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class report : MonoBehaviour
{
    public GameObject Reports;
    public GameObject xchart;

    public Text[] Text_1;
    public Text[] Text_2;
    public Text[] Text_3;
    double[] force =new double[3];
    double[] Acceleration = new double[3];
    void Start()
    {

        //  Debug.Log(position);
        //   calculate();
       
        Reports.SetActive(false);
    }

    public void fillingData() {
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



    public void exit() {
        Reports.SetActive(false);
    }

    public void save()
    {
        xchart.SetActive(true);
        FindObjectOfType<controlData>().setData(force,Acceleration);
    }


}
