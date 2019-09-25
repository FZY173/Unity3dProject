using UnityEngine;
using System.Collections;
using UnityEngine.UI;//注意这个不能少
//using UnityEditor.Sprites ;
public class packUp : MonoBehaviour
{
    //public GameObject Gmenue;
    public GameObject packUpButton;
    public GameObject prompts;
    public Text ddescriptions;
    private Vector3 promptsPosition;
    int isshow = 0;
    void Start()
    {
        //prompts.transform.position = new Vector3(-110,557.7f,0);
        promptsPosition = prompts.transform.localPosition;
        packUpButton.SetActive(true);
        ddescriptions.text = "展开";

    }

    void Update()
    {
        if (isshow ==1) {
            ddescriptions.text = "收起";
            Vector3 distance = prompts.transform.position - promptsPosition;
           // Debug.Log(distance.x);
            if (distance.x >= 777) { 
                prompts.transform.position -= prompts.transform.right * 200 * Time.deltaTime;
            }

        }
        if (isshow ==2) {
            ddescriptions.text = "展开";
            Vector3 distance = prompts.transform.position - promptsPosition;
          //  Debug.Log(distance.x);
            if (distance.x <= 965)
            {
                prompts.transform.position += prompts.transform.right * 200 * Time.deltaTime;
            }
           
        }
        if (isshow == 3) {
            isshow = 1;
        }
    }

    public void showPeomptsClick() {
        isshow++;
    }
}

