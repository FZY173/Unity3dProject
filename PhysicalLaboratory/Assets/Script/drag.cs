
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using UnityStandardAssets.Characters.FirstPerson;

public class drag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject personMainCamera;
    public GameObject farmar;
    public GameObject[] famarPosition;
    public GameObject parent;
    bool isMove = true;
    bool isShow = false;
    Vector3 distance;

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        isShow = true;
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;//eventData就是屏幕坐标下的鼠标位置
        RaycastHit hit = new RaycastHit();
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out hit);
        distance = hit.point - new Vector3(0, 0, 0);
        isShow = true;
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        GameObject.Find("fama-1").transform.position = new Vector3(Screen.width * 9.4994f / 10, Screen.height * 8.0f / 10, 0);
        GameObject.Find("fama-2").transform.position = new Vector3(Screen.width * 9.4994f / 10, Screen.height * 6.5f / 10, 0);
        GameObject.Find("fama-5").transform.position = new Vector3(Screen.width * 9.4994f / 10, Screen.height * 5.0f / 10, 0);
        GameObject.Find("fama-10").transform.position = new Vector3(Screen.width * 9.4994f / 10, Screen.height * 3.0f / 10, 0);
        if (distance.x > -4f && distance.x < -1f && distance.y > -4f && distance.y < -1f && parent.transform.childCount <= 6)
        {
            if (isShow && !FindObjectOfType<startReset>().isMove)
            {

                GameObject EnemyObj = GameObject.Instantiate(farmar, famarPosition[Define.currentIndex].transform.position, famarPosition[Define.currentIndex].transform.rotation);
                EnemyObj.transform.parent = parent.transform;
                Define.currentIndex++;
                FindObjectOfType<famarControl>().AccountFamar();

            }
            isShow = false;

        }

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
    }
}

