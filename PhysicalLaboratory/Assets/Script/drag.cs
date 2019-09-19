
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using UnityStandardAssets.Characters.FirstPerson;

public class drag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject personMainCamera;
    public GameObject farmar;
    bool isMove = true;
    bool isShow = false;
    Vector3 distance ;
   
    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        print("start drag");
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;//eventData就是屏幕坐标下的鼠标位置
            RaycastHit hit = new RaycastHit();
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out hit);
             distance = hit.point - new Vector3(0, 0, 0);
        // Debug.Log("distance.x" + distance.x);
        // Debug.Log("distance.y" + distance.y);
        isShow = true;
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        print("end drag");
        GameObject.Find("juanchi").transform.position = new Vector3(766.0f, 305.0f, 0.0f);
        GameObject.Find("fama").transform.position = new Vector3(760.0f, 197.5f, 0.0f);
        
    }

    void Start() {
        distance.x = -2;
    }
    void Update()
    {
        personMainCamera.GetComponent<FirstPersonController>().enabled = isMove;
        if (Input.GetKeyDown(KeyCode.K))
        {
            isMove = !isMove;
        }
        if (distance.x > -4f && distance.x < -1f && distance.y > -4f && distance.y < -1f )
        {
            if (isShow) {
                Instantiate(farmar);
                farmar.transform.position = new Vector3(0.88f, -0.42f, 0.799f);
            }
            isShow = false;

        }

    }
}

