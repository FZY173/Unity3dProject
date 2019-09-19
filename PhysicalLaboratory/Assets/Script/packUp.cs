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
    int isshow = 0;
    void Start()
    {
        prompts.transform.position =new Vector3(-110,557.7f,0);
        packUpButton.SetActive(true);
        ddescriptions.text = "展开";

    }

    void Update()
    {
        if (isshow ==1) {
            ddescriptions.text = "收起";
            Vector3 distance = prompts.transform.position - new Vector3(-110, 67.7f,0);
            if (distance.x <= 218) {
                prompts.transform.position += prompts.transform.right * 100 * Time.deltaTime;
            }
        }
        if (isshow ==2) {
            ddescriptions.text = "展开";
            Vector3 distance = prompts.transform.position - new Vector3(-110, 67.7f, 0);
            if (distance.x >= 0f)
            {
                prompts.transform.position -= prompts.transform.right * 100 * Time.deltaTime;
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

