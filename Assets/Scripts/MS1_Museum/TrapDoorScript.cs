using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapDoorScript : MonoBehaviour {

    private static List<TrapDoorScript> trapDoors = new List<TrapDoorScript>(); //list of all instances

    public float XOffset = 0.54f;
    public float beginTimeOffset = 0.0f;
    public float openingTime = 5.0f;
    public float closingTime = 5.0f;
    public float waitTime = 10.0f;

    // Use this for initialization
    void Start () {

        trapDoors.Add(this);
    }

    private void OnDestroy()
    {
        trapDoors.Remove(this);
    }

    // Update is called once per frame
    void Update () {
		
	}

    private IEnumerator OpenTrapDoor()
    {
        yield return new WaitForSeconds(beginTimeOffset);

        Vector3 origPosition = transform.position;
        float timer = 0f;

        while (timer < openingTime)
        {
            Vector3 newPosition = origPosition;
            newPosition.x = Mathf.Lerp(origPosition.x, origPosition.x + XOffset, Mathf.Sin(timer / openingTime * Mathf.PI / 2.0f));
            transform.position = newPosition;

            timer += Time.deltaTime;

            yield return null;
        }

        //wait a bit
        yield return new WaitForSeconds( waitTime );

        //closing time
        timer = 0f;

        while (timer < openingTime)
        {
            Vector3 newPosition = origPosition;
            newPosition.x = Mathf.Lerp(origPosition.x, origPosition.x + XOffset, Mathf.Sin((1.0f - timer / closingTime) * Mathf.PI / 2.0f));
            transform.position = newPosition;

            timer += Time.deltaTime;

            yield return null;
        }

        transform.position = origPosition;
    }

    public static void StartTrapDoorSequence()
    {
        foreach( TrapDoorScript tds in trapDoors )
        {
            tds.StartCoroutine(tds.OpenTrapDoor());
        }
    }
}
