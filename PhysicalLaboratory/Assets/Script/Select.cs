using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Select : MonoBehaviour
{
    
    public AudioClip[] music;//音乐集
    private Dropdown.OptionData[] data ;
    public Slider volume_slider;
    public GameObject volume;
    [HideInInspector]
    public int index;
    public AudioSource musicCurrt;//当前播放的音乐
    public GameObject obj;
     Dropdown dpn;
    private bool showDown = false;
    private bool showVolun = false;


    void Start()
    {
        Globes.index = index;
        dpn = obj.GetComponent<Dropdown>();
        musicCurrt = musicCurrt.GetComponent<AudioSource>();
        volume_slider.value = 0.5f;
        musicCurrt.clip = music[0];
        musicCurrt.Play();

        for (int i = 0; i < music.Length; i++)
        {
            add(music[i].name);
        }
        
    }

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
                    Globes.index = i;
                    musicCurrt.Play();
                    Debug.Log(i);
                  
                   
                }
            }
        }
        

    }

    public void play()
    {
        showDown = !showDown;
        obj.SetActive(showDown);
    }

    public void volunShow()
    {
        showVolun = !showVolun;
        volume.SetActive(showVolun);
    }

    public void volunZero()
    {
        volume_slider.value = 0f;
    }
}
