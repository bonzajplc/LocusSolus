using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestalScript : MonoBehaviour {

    private static List<PedestalScript> pedestals = new List<PedestalScript>(); //list of all instances

    public float YOffset = 2.0f;
    public float beginTimeOffset = 0.0f;
    public float openingTime = 10.0f;
    public float waitTime = 5.0f;
    public float closingTime = 8.0f;

    // Use this for initialization
    void Start()
    {
        pedestals.Add(this);
    }

    private void OnDestroy()
    {
        pedestals.Remove(this);
    }

    private IEnumerator OpenPedestal()
    {
        yield return new WaitForSeconds(beginTimeOffset);

        Vector3 origPosition = transform.position;
        float timer = 0f;

        while (timer < openingTime)
        {
            Vector3 newPosition = origPosition;
            newPosition.y = Mathf.Lerp(origPosition.y, origPosition.y + YOffset, Mathf.Sin(timer / openingTime * Mathf.PI / 2.0f));
            transform.position = newPosition;

            timer += Time.deltaTime;

            yield return null;
        }

        //wait a bit
        yield return new WaitForSeconds(waitTime);

        //closing time
        timer = 0f;

        while (timer < openingTime)
        {
            Vector3 newPosition = origPosition;
            newPosition.y = Mathf.Lerp(origPosition.y, origPosition.y + YOffset, Mathf.Sin((1.0f - timer / closingTime) * Mathf.PI / 2.0f));
            transform.position = newPosition;

            timer += Time.deltaTime;

            yield return null;
        }

        transform.position = origPosition;
    }

    public static void StartPedestalSequence()
    {
        foreach (PedestalScript pedestal in pedestals)
        {
            pedestal.StartCoroutine(pedestal.OpenPedestal());
        }
    }
}
