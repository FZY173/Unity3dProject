using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class startReset : MonoBehaviour
{
    public GameObject car;
    public GameObject pallet;
    public Text text;
    bool isMove = false;
    public GameObject Rope_x;
    public GameObject Rope_y;
    public GameObject pulley;

    public float force;

    //位置
    private Vector3 carPosition;
    private Vector3 palletPosition;
    private Vector3 rope_xPosition;
    private Vector3 rope_yPosition;
    private Vector3 rope_xScale;
    private Vector3 rope_yScale;
    public float speed = 2f;
    public bool isRotation = false;




    void Start()
    {
        //car.GetComponent<ConstantForce>().force = new Vector3(0, 0, 0);
        text.text = "开始";
        carPosition = car.transform.localPosition;
        palletPosition = pallet.transform.position;
        rope_xPosition = Rope_x.transform.position;
        rope_yPosition = Rope_y.transform.position;
        //尺寸
        rope_xScale = Rope_x.transform.localScale;
        rope_yScale = Rope_y.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        //旋转
        if (isRotation)
        {
           pulley.transform.Rotate(new Vector3(0, -1, 0) * speed);
        }


        if (isMove)
        {
            text.text = "重置";
            car.GetComponent<ConstantForce>().force = new Vector3(0, 0, force);
            pallet.GetComponent<ConstantForce>().force = new Vector3(0, -force, 0);
        }
        else {
           // car.transform.localPosition = new Vector3(-42.09993f, 9.929489f, -3.725288e-06f);
            car.transform.localPosition = carPosition;
            pallet.transform.position = palletPosition;
            text.text = "开始";
            car.GetComponent<ConstantForce>().force = new Vector3(0, 0, 0);
            pallet.GetComponent<ConstantForce>().force = new Vector3(0, 0, 0);
            Rope_x.transform.position = rope_xPosition;
            Rope_y.transform.position = rope_yPosition;
            //尺寸
            Rope_x.transform.localScale = rope_xScale;
            Rope_y.transform.localScale = rope_yScale;

        }
    }

    public void carStart() {
        if (FindObjectOfType<famarControl>().Mass != 0)
        {
            isMove = !isMove;
        }
    }
}
