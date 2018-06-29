using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintingScript : MonoBehaviour {

    public float flightTime = 3.0f;
    public float waitTime = 3.0f;
    public float scaleTime = 1.5f;

    private static int flyingPaintingsCount = 0;
    private Vector3 origScale = Vector3.one;

    private static List<PaintingScript> paintings = new List<PaintingScript>(); //list of all instances

    // Use this for initialization
    void Start () {
        //inject process hit method
        LookAtObject lao = GetComponent<LookAtObject>();
        lao._processObjectMethodToCall = ProcessObject;

        origScale = transform.localScale;

        paintings.Add(this);
    }

    private void OnDestroy()
    {
        paintings.Remove(this);
    }

    public void ProcessObject(GameObject go)
    {
        flyingPaintingsCount++;

        StartCoroutine(ActivatePainting());
    }

    // Update is called once per frame
    public IEnumerator ActivatePainting()
    {
        Vector3 targetPosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
        Quaternion targetRotation = Quaternion.LookRotation( Camera.main.transform.forward, Vector3.up ) * Quaternion.AngleAxis( 180.0f, Vector3.up);

        Vector3 origPosition = transform.position;
        Quaternion origRotation = transform.rotation;

        LookAtObject.lockCameraRaycast = true;

        float timer = 0f;

        while (timer < flightTime)
        {
            float curTime = Mathf.SmoothStep(0.0f, 1.0f, timer/flightTime);

            transform.position = Vector3.Lerp(origPosition, targetPosition, curTime);
            transform.rotation = Quaternion.Slerp(origRotation, targetRotation, curTime);

            timer += Time.deltaTime;

            yield return null;
        }

        //wait a bit
        yield return new WaitForSeconds(waitTime);

        //scale while flying back
        timer = 0f;

        while (timer < flightTime)
        {
            LookAtObject.lockCameraRaycast = true;
            float curTime = Mathf.SmoothStep(0.0f, 1.0f, timer / flightTime);
            float curScaleTime = Mathf.Max( 0.0f, 1.0f - timer / scaleTime );

            transform.position = Vector3.Lerp(targetPosition, origPosition, curTime);
            transform.rotation = Quaternion.Slerp(targetRotation, origRotation, curTime);
            transform.localScale = origScale * curScaleTime;

            timer += Time.deltaTime;

            yield return null;
        }

        if( flyingPaintingsCount == 3 )
        {
            //scale all remaining paintings 
            timer = 0f;

            while (timer < scaleTime)
            {
                float curScaleTime = Mathf.Max(0.0f, 1.0f - timer / scaleTime);
                foreach (PaintingScript painting in paintings)
                {
                    if( painting.gameObject.activeSelf && painting != this )
                        painting.transform.localScale = painting.origScale * curScaleTime;
                }

                timer += Time.deltaTime;

                yield return null;
            }

            foreach (PaintingScript painting in paintings)
            {
                painting.gameObject.SetActive(false);
            }

            //open trapdoors
            TrapDoorScript.StartTrapDoorSequence();

            PedestalScript.StartPedestalSequence();
        }

        LookAtObject.lockCameraRaycast = false;
    }
}
