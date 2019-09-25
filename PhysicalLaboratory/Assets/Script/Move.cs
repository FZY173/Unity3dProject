using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;



public class Move : MonoBehaviour
{
    public GameObject obj_1;
    public GameObject obj_2;
    private float cube_y = -2.116f;
    private float speed;
    private float speedCount;

    public double speedFirst =0;
    public double speedSecont =0;

    public Text doorSpeed1;
    public Text doorSpeed2;

    public GameObject door1;
    public GameObject door2;
    private double position;
    decimal result;


    public bool start =true;

    // Start is called before the first frame update
    void Start()
    {
        position = door2.transform.position.z - door1.transform.position.z;
    }

    // Update is called once per frame

    void FixedUpdate()
    {
        speedCount = this.GetComponent<Rigidbody>().velocity.z;
        speed = speedCount / 100f;

        if (Input.GetKeyDown(KeyCode.Z))
        {
          calculate();
        }

    }
    /*
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("obi"))
        {

            //Debug.Log(other.gameObject);
            Destroy(other.gameObject);
            Instantiate(obj, new Vector3(-4.898f, cube_y, 3.4f), Quaternion.LookRotation(new Vector3(-90f, 0, 0)));
            cube_y -= 0.05f;
        }
    }
    */

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("obi"))
        {
            obj_1.transform.localScale -= new Vector3(0, 0, speed);
            obj_2.transform.localScale += new Vector3(0, 0, speed);
            FindObjectOfType<startReset>().isRotation = true;
        }

        if (other.gameObject.CompareTag("wall")&& start)
        {
            FindObjectOfType<startReset>().isRotation = false;
            FindObjectOfType<report>().fillingData();
            start = false;
        }

        if (other.gameObject.CompareTag("door2"))
        {
            speedSecont = speedCount;
            doorSpeed2.text = speedSecont.ToString();     
            // Debug.Log(speedSecont);
        }

        if (other.gameObject.CompareTag("door1"))
        {
            speedFirst = speedCount;
            doorSpeed1.text = speedFirst.ToString();
            start = true;
         //   Debug.Log(speedFirst);
        }
    }

    /*void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("obi"))
        {
        //    obj_1.transform.localScale -= new Vector3(0, 0, 0.001f);
            //obj_2.transform.localScale += new Vector3(0, 0, 0.001f);
        }
    }*/

    public double calculate()
    {
      //  double a = (Math.Pow(speedSecont, 2) - Math.Pow(speedFirst, 2)) / (2 * position);
        // Acceleration.Add();
      //  Debug.Log((Math.Pow(speedSecont, 2) - Math.Pow(speedFirst, 2)));
      //  Debug.Log((Math.Pow(speedSecont, 2) - Math.Pow(speedFirst, 2)) / (2 * position));
      //  Debug.Log((Math.Pow(speedSecont, 2) - Math.Pow(speedFirst, 2)) / (2 * position));
        // decimal b = Math.Round((decimal)a, 1, MidpointRounding.AwayFromZero);
        //DecimalMath.Pow(val1, val2);
        //Debug.Log(b);
        return (Math.Pow(speedSecont, 2) - Math.Pow(speedFirst, 2)) / (2 * position);
    }
}
