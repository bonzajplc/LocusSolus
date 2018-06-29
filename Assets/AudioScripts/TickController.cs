using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickController : MonoBehaviour {

    private AudioSource audioSource =   null;
    private bool        tick        =   false;
    private float       clip_t      =   0.0f;

    [Range(0.0f, 1.0f)]
    public float probability = 1.0f;

    float tickValue = 0.0f;
    DensityFieldInstancer instancer = null;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
            clip_t = audioSource.clip.length;

        instancer = GetComponentInParent<DensityFieldInstancer>();
    }

	void Update()
    {
        if (clip_t < 0)
        {
            if (tick)
            {
                Tick();
            }

            tick = Random.value < probability;
            clip_t = audioSource.clip.length;
        }

        clip_t -= Time.deltaTime;
        tickValue -= 2 * Time.deltaTime;
        tickValue = Mathf.Clamp01(tickValue);

        if (instancer)
        {
            if (instancer.matPropertyBlock != null)
                instancer.matPropertyBlock.SetFloat("_NoiseAmount", tickValue);
        }
    }

    void
    Tick()
    {
        audioSource.Play();
        tickValue = 1.0f;
    }
}
