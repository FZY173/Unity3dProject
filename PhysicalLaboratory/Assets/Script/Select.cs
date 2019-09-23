using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Select : MonoBehaviour
{
    Dropdown dpn;
    public AudioClip[] music;//音乐集
    private Dropdown.OptionData[] data ;
    private AudioSource musicCurrt;//当前播放的音乐
    public Slider volume_slider;
    public GameObject musicGamnObject;

    // Start is called before the first frame update
    void Start()
    {
        dpn = transform.GetComponent<Dropdown>();
        musicCurrt = musicGamnObject.GetComponent<AudioSource>();

        volume_slider.value = 0.5f;
        musicCurrt.clip = music[0];
        musicCurrt.Play();

        for (int i = 0; i < music.Length; i++)
        {
            add(music[i].name);
        }
        //Debug.Log(music[0].name);
    }

    // Update is called once per frame
    void Update()
    {
        musicCurrt.volume = volume_slider.value;
    }

    public void add(string a)
    {
        Dropdown.OptionData data = new Dropdown.OptionData();
        data.text = a;
        dpn.options.Add(data);
    }


    public void Drop_select(int n)
    {
        if (dpn.captionText.text != musicCurrt.clip.name)
        {
            for (int i = 0; i < music.Length; i++)
            {
                if (music[i].name == dpn.captionText.text)
                {
                    musicCurrt.clip = music[i];
                    musicCurrt.Play();
                }
            }
        }
        

    }
}
