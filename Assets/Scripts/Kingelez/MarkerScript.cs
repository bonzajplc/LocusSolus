/************************************************************************************

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Licensed under the Oculus VR Rift SDK License Version 3.2 (the "License");
you may not use the Oculus VR Rift SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

http://www.oculusvr.com/licenses/LICENSE-3.2

Unless required by applicable law or agreed to in writing, the Oculus VR SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

************************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MarkerScript : MonoBehaviour {

    public GameObject sceneScriptObject;

    Kingelez_SceneScript sceneScript = null;

    Kingelez_SceneScript Kingelez_SceneScript()
    {
        if( sceneScript == null )
            sceneScript = FindObjectOfType<Kingelez_SceneScript>();

        return sceneScript;
    }

    public int markerLevel = 0;
    private Vector3 origPosition = Vector3.zero;

    public Vector3 GetOrigPosition { get { return origPosition; } }

    private Vector3 origScale = Vector3.one;
    private float markerHeight = 1.0f;

    private static List<MarkerScript> markers = new List<MarkerScript>(); // List of all instances

    // Occlusion collision
    // Collision actor for occlusion (for testing if there is more than one marker straight ahead from camera).
    // Different than collision component attached to the marker, which is supposed to scale together with it during "show up" animation.
    public GameObject occlusionCollisionPrefab = null;
    [HideInInspector]
    public GameObject occlusionCollision = null;
    private Vector3 origScaleOcclusionCollision = Vector3.one;

    // Use this for initialization
    void Start () {
        origScale = transform.localScale;
        origPosition = transform.position;

        CapsuleCollider cc = GetComponent<CapsuleCollider>();
        markerHeight = cc.height * 0.5f + cc.radius;

        //inject process hit method
        LookAtObject lao = GetComponent<LookAtObject>();
        lao._processObjectMethodToCall = ProcessObject;

        InitMarker();

        // Occlusion collision
        if (occlusionCollisionPrefab != null)
        {
            occlusionCollision = Instantiate(occlusionCollisionPrefab, transform.position, Quaternion.identity);
            origScaleOcclusionCollision = occlusionCollision.transform.localScale;
        }
        else
        {
            occlusionCollision = null;
        }
        
        markers.Add(this);
    }

    void InitMarker()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    public static void InitAllMarkers()
    {
        foreach(var marker in markers)
        {
            marker.InitMarker();
        }
    }

    private void OnDestroy()
    {
        markers.Remove(this);
    }

    public static GameObject FindClosestMarker(Vector3 currentPos, int markerLevel)
    {
        float curDistance = 10000.0f;
        GameObject closestMarker = null;

        foreach (MarkerScript marker in markers)
        {
            if (marker.markerLevel != markerLevel)
                continue;

            float markerDistance = Vector3.Magnitude(marker.origPosition - currentPos);
            if( markerDistance < curDistance)
            {
                curDistance = markerDistance;
                closestMarker = marker.gameObject;
            }
        }

        return closestMarker;
    }

    public static IEnumerator ScaleMarkers( MarkerScript selectedMarker, int newMarkerLevel, Vector3 newPosition, float markerScaleDuration, float markerScaleTimeOffset, bool microScale ) 
    {
        // This happens in total blackness
        float additionalScale = microScale ? 0.5f : 1.0f;

        foreach (MarkerScript marker in markers)
        {
            if (marker.markerLevel != newMarkerLevel || marker == selectedMarker)
            {
                marker.gameObject.SetActive(false);
                continue;
            }

            float markerDistance = Vector3.Magnitude(marker.origPosition - newPosition);

            float maxDistance = 20.0f;

            if (marker.markerLevel == 0)
            {
                maxDistance = marker.Kingelez_SceneScript().maxDistanceLevel0;
            }
            else if (marker.markerLevel == 1)
            {
                maxDistance = marker.Kingelez_SceneScript().maxDistanceLevel1;
            }
            else if (marker.markerLevel == 2)
            {
                maxDistance = marker.Kingelez_SceneScript().maxDistanceLevel2;
            }
            else if (marker.markerLevel == 3)
            {
                maxDistance = marker.Kingelez_SceneScript().maxDistanceLevel3;
            }

            if ( markerDistance < 0.1f || markerDistance > maxDistance )
            {
                marker.gameObject.SetActive(false);
                continue;
            }


            Vector3 finalMarkerPosition = marker.origPosition - marker.origScale.x * Vector3.up * marker.markerHeight * (1.0f - 1.0f * additionalScale);
            Vector3 finalSelectedMarkerPosition = Vector3.zero;
            if (marker.markerLevel != 0)
            {
                finalSelectedMarkerPosition = selectedMarker.origPosition - selectedMarker.origScale.x * Vector3.up * selectedMarker.markerHeight * (1.0f - 1.0f * additionalScale);
            }

            // Occlusion collision
            if (marker.occlusionCollision != null)
            {
                marker.occlusionCollision.transform.localScale = marker.origScaleOcclusionCollision * additionalScale; // Make Occlusion Collision the same size
                marker.occlusionCollision.transform.position = finalMarkerPosition;
            }

            // Create a ray that points from marker to the player (ground level [newPosition]) and let's check if it's obstructed by another marker
            if (marker.markerLevel != 0)
            {
                Ray ray = new Ray(finalMarkerPosition, Vector3.Normalize(newPosition - finalMarkerPosition));
                RaycastHit hit;
                int layerMask = 1 << 13; // "MarkerOcclusionCollision" layer

                if (Physics.SphereCast(ray, 0.3f, out hit, markerDistance, layerMask))
                {
                    if (hit.transform.gameObject.tag == "MarkerOcclusionCollision")
                    {
                        //Debug.DrawRay(finalMarkerPosition, newPosition - finalMarkerPosition, Color.green, 1000.0f, true);
                        //Debug.DrawRay(finalMarkerPosition, hit.point - finalMarkerPosition, Color.yellow, 1000.0f, true);

                        marker.gameObject.SetActive(false);
                        continue;
                    }
                }
                else
                {
                    //Debug.DrawRay(finalMarkerPosition, newPosition - finalMarkerPosition, Color.red, 1000.0f, true);
                }
            }

            // Create a ray once again (from final position to final position this time), to check if marker is not behind building (it shouldn't be visible behind glass)
            if (marker.markerLevel == 3)
            {
                RaycastHit hit2;
                int layerMask2 = 1 << 9; // "Building" layer

                if (Physics.Linecast(finalMarkerPosition, finalSelectedMarkerPosition, out hit2, layerMask2, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawRay(finalMarkerPosition, finalSelectedMarkerPosition - finalMarkerPosition, Color.green, 1000.0f, true);

                    marker.gameObject.SetActive(false);
                    continue;
                }
                else
                {
                    Debug.DrawRay(finalMarkerPosition, finalSelectedMarkerPosition - finalMarkerPosition, Color.red, 1000.0f, true);
                }
            }

            marker.gameObject.SetActive(true);
            marker.transform.localScale = Vector3.zero;
            marker.transform.position = marker.origPosition;

            if (marker.occlusionCollision != null)
            {
                marker.occlusionCollision.gameObject.SetActive(true);
            }
        }

        yield return new WaitForSeconds(markerScaleTimeOffset);

		// Execute this loop once per frame until the timer exceeds the duration.
		float timer = 0f;
		while (timer <= markerScaleDuration)
		{
			foreach (MarkerScript marker in markers)
			{
                if( marker.markerLevel == newMarkerLevel )
                {
                    float ratio = Mathf.Sin(Mathf.PI * 0.5f * timer / markerScaleDuration);
                    marker.transform.localScale = ratio * marker.origScale * additionalScale;
                    marker.transform.position = marker.origPosition - marker.origScale.x * Vector3.up * marker.markerHeight * ( 1.0f - ratio * additionalScale );
				}
    		}

			// Increment the timer by the time between frames and return next frame.
			timer += Time.deltaTime;
			yield return null;
		}

        // To hide older levels
        foreach (MarkerScript marker in markers)
        {
            if (marker.markerLevel == newMarkerLevel)
            {
                marker.transform.localScale = marker.origScale * additionalScale;
            }
        }
    }
	
	public void ProcessObject(GameObject parentCanvas)
	{
		if (sceneScriptObject == null)
			return;

        if (parentCanvas)
        {
            Canvas canvas = parentCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }
		
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        int layerMask = 1 << 2; //ignore raycast

        bool raycastDown = markerLevel == 0 ? false : true;

        Vector3 hitPoint = transform.position;

        if ( raycastDown && Physics.Raycast(ray, out hit, 100.0f, ~layerMask) )
        {
            hitPoint = hit.point;
        }
        else
        {
            hitPoint.y = 0.0f;
        }

        Kingelez_SceneScript ss = sceneScriptObject.GetComponent<Kingelez_SceneScript>();

        if (ss != null)
        {
            //ss.currentTimer += 5.0f;
            ss.TeleportCharacterController(gameObject, hitPoint);
        }
    }
}
