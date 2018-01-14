using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestalController : MonoBehaviour {

    [SerializeField]
    AudioSource infoAudioSource;

    [SerializeField]
    float audioLoopInterval = 2.0f;
    float audioLoopCounter;

    bool playerInTrigger;
    

    // Use this for initialization
    void Start()
    {
        playerInTrigger = false;        
    }

    // Update is called once per frame
    void Update () {
        if (playerInTrigger)
        {
            if (!infoAudioSource.isPlaying)
            {
                if (audioLoopCounter <= 0)
                {
                    StopCoroutine("AudioFadeOut");
                    infoAudioSource.Play();
                    audioLoopCounter = audioLoopInterval;
                }
                else
                {
                    audioLoopCounter -= Time.deltaTime;
                }
            }
        }
	}

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {   
            //Debug.Log("Player enters with the pedestal trigger!");
            if(infoAudioSource != null)
            {
                StopCoroutine("AudioFadeOut");
                infoAudioSource.Play();
                audioLoopCounter = audioLoopInterval;
            }
            
            playerInTrigger = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            playerInTrigger = false;

            //Debug.Log("Player exits with the pedestal trigger!");
            if (infoAudioSource != null)
            {
                //infoAudioSource.Stop();
                StartCoroutine(AudioFadeOut(infoAudioSource, 0.5f));
            }
                
        }
    }

    IEnumerator AudioFadeOut(AudioSource audioSource, float FadeTime)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / FadeTime;

            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    
}
