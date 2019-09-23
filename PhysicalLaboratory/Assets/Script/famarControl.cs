using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class famarControl : MonoBehaviour
{

    public GameObject Parent;

    public int Mass = 0;

    //砝码的质量
    public int famarMass1;
    public int famarMass2;
    public int famarMass3;
    public int famarMass4;

    private List<GameObject> Children = new List<GameObject>();

    public Text famarMass;



    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

   

    public void DefineFamar()
    {
        /* for (int i = 0; i < Parent.transform.childCount; i++)
         {
            // Children.Add(Parent.transform.GetChild(i).gameObject);
             //Debug.Log(Children[i].name);
            // Destroy(Parent.transform.GetChild(i).gameObject);
            // Define.currentIndex = 0;
         }*/

        if (Children != null)
        {
            foreach (var child in Children)
            {
                Destroy(child);
                // Debug.Log(child.name);
            }

            Define.currentIndex = 0;
            Mass = 0;
            famarMass.text = "砝码质量：" + "0.0" + "kg";
        }
    }


    public void AccountFamar()
    {
        int mass = 0;
        for (int i = 0; i < Parent.transform.childCount; i++)
        {
            Children.Add(Parent.transform.GetChild(i).gameObject);

            //Debug.Log(Parent.transform.GetChild(i).gameObject.tag);
            switch (Parent.transform.GetChild(i).gameObject.tag)
            {
                case ("famar01"):
                    mass += famarMass1;
                    break;
                case ("famar02"):
                    mass += famarMass2;
                    break;
                case ("famar03"):
                    mass += famarMass3;
                    break;
                case ("famar04"):
                    mass += famarMass4;
                    break;
            }
        }

        Mass = mass;
        FindObjectOfType<startReset>().force = (Mass/100f) * 10;
        famarMass.text = "砝码质量：" + Mass/100f + "kg";
       // Debug.Log((Mass / 100f) * 10);
    }
}
