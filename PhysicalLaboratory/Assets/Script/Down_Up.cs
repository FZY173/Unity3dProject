using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Down_Up : MonoBehaviour
{
    public Transform Up;
    public Transform Down;
    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Up.position += Up.up * speed * Time.deltaTime;
        Down.position -= Down.up * speed * Time.deltaTime;
    }
}
