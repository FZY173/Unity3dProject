using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public GameObject obj_1;
    public GameObject obj_2;
    private float cube_y = -2.116f;
    private float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(0, 0.0f, moveHorizontal);

        transform.position += movement*0.01f;
        speed = this.GetComponent<Rigidbody>().velocity.z/100f;


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
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("obi"))
        {
        //    obj_1.transform.localScale -= new Vector3(0, 0, 0.001f);
            //obj_2.transform.localScale += new Vector3(0, 0, 0.001f);
        }
    }
}
