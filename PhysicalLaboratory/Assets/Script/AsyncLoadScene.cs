using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
 
public class Globe
{
    public static string nextSceneName;
}
 
public class AsyncLoadScene : MonoBehaviour
{
    public Slider loadingSlider;    //加载场景的进度条
   
    public Text loadingText;        //加载场景时的百分比
 
    private float loadingSpeed = 1;
 
    private float targetValue;
 
    private AsyncOperation operation;    //异步操作

    // Use this for initialization
    void Start ()
    {
        loadingSlider.value = 0.0f;   //初始化Slider
 
        if (SceneManager.GetActiveScene().name == "LoadingScene")
        {
            //启动协程
            StartCoroutine(AsyncLoading());
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
    
    IEnumerator AsyncLoading()
    {
        operation = SceneManager.LoadSceneAsync(Globe.nextSceneName);
        //阻止当加载完成自动切换
        operation.allowSceneActivation = false;
        yield return operation;
    }
	
    // Update is called once per frame
    void Update ()
    {
        targetValue = operation.progress;
 
        if (operation.progress >= 0.9f)
        {
            //operation.progress的值最大为0.9
            targetValue = 1.0f;
        }

//        loadingSlider.value = targetValue;
 
        //平滑加载
        if (targetValue != loadingSlider.value)
        {
            //插值运算
            loadingSlider.value = Mathf.Lerp(loadingSlider.value, targetValue, Time.deltaTime * loadingSpeed);
            if (Mathf.Abs(loadingSlider.value - targetValue) < 0.01f)
            {
                loadingSlider.value = targetValue;
            }
        }
	
        loadingText.text = ((int)(loadingSlider.value * 100)).ToString() + "%";
 
        if ((int)(loadingSlider.value * 100) == 100)
        {
            //允许异步加载完毕后自动切换场景
            operation.allowSceneActivation = true;
        }
    }
}