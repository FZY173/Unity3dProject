using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreatCube : MonoBehaviour
{
    public GameObject obj;
    private float cube_z = -1.6f;
    private float cube_y = -1.116f;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 100; i++)
        {
            cube_z += 0.05f;
            Instantiate(obj, new Vector3(-4.898f, -1.116f, cube_z), Quaternion.identity);

        }
        for (int i = 0; i < 20; i++)
        {
            
            Instantiate(obj, new Vector3(-4.898f, cube_y, cube_z), Quaternion.LookRotation(new Vector3(-90f,0, 0)));
            cube_y -= 0.05f;

        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
