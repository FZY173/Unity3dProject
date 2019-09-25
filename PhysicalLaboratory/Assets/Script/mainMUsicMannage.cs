using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Globes
{
    public static int index;
}
public class mainMUsicMannage : MonoBehaviour
{
    private AudioSource musicCurrt;//当前播放的音乐
    public AudioClip[] music;//音乐集
   // private int index ;


    void Start()
    {
       
        musicCurrt = this.GetComponent<AudioSource>();
     
        musicCurrt.clip = music[Globes.index];
        musicCurrt.Play();

    }

    void Update()
    {
       
    }

   
                    
}
