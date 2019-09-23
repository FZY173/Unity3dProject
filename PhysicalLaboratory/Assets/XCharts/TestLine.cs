using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLine : MonoBehaviour
{
    private LineRenderer lineRenderer;


    void Start()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();

        lineRenderer.startColor = Color.red;    //折线开始时的颜色
        lineRenderer.endColor = Color.red;      //折现结束时的颜色
        lineRenderer.startWidth = 3f;        //折线开始时的宽度
        lineRenderer.endWidth = 3f;          //折线结束时的宽度
        lineRenderer.useWorldSpace = false;     //使用世界坐标还是本地坐标

        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, new Vector3(0, 0));
        lineRenderer.SetPosition(1, new Vector3(10, 10));
        lineRenderer.SetPosition(2, new Vector3(20, 20));
        lineRenderer.SetPosition(3, new Vector3(30, 30));
        lineRenderer.SetPosition(4, new Vector3(40, 40));

        AddPosition(new Vector3(50, 50));
        AddPosition(new Vector3(60, 60));
        AddPosition(new Vector3(70, 0));
    }

    // 此处需要注意的是，使用此方法前必须得保证前面至少已经有了一条直线，也就是说已经设置了两个点，不然
    // 系统会默认前两个点的位置为 Vector3.zero
    public void AddPosition(Vector3 v3)
    {
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, v3);
    }

    public void ResetPosition(int index, Vector3 v3)
    {
        if (0 <= index && index <= lineRenderer.positionCount)
        {
            lineRenderer.SetPosition(index, v3);
        }
    }

    // 重新绘制一条折线，会替换原有的折线
    public void ResetPositions(Vector3[] v3s)
    {
        lineRenderer.SetPositions(v3s);
    }
}