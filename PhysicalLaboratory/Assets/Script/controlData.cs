using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XCharts;

public class controlData : MonoBehaviour
{
    private  Transform xchart;
    private BaseChart chart;

    void Awake()
    {
        xchart = this.gameObject.transform;
        chart = xchart.gameObject.GetComponentInChildren<BaseChart>();
    }

    

    public void setData(double[] force,double[] add)
    {
        chart.ClearData();
        chart.AddData(0, 0, 0);
        for(int i = 0; i < 3; i++)
        {
            chart.AddData(0, (float)add[i], (float)force[i]);
        }
    }

}
