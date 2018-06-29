using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitchController : MonoBehaviour
{
    public LocusSolus_teaser scene;
    public float bias = 0.1f;
    private AudioSource[] audioSources = new AudioSource[2];
    
	void Start()
    {
        audioSources = GetComponents<AudioSource>();
	}

	void Update()
    {
        float pitch = scene.outputAxis;

        audioSources[0].pitch = 1 - pitch;
        audioSources[1].pitch = pitch;

        if (!audioSources[0].isPlaying)

            audioSources[0].Play();

        if (!audioSources[1].isPlaying)

            audioSources[1].Play();

         if (pitch < bias)
        {
            audioSources[0].Stop();
            audioSources[1].Stop();
        }
    }
}
