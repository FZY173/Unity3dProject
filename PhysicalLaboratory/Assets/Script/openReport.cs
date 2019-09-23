using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class openReport : MonoBehaviour

{
    public GameObject report;
    void Start()
    {
      
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            //从鼠标位置发送射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out hit);
           
            if (hit.collider.gameObject.name == "Plane03") {
                report.SetActive(true);
            }
        }
    }
}
