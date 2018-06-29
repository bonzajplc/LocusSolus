using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class Kingelez_SceneScript : MonoBehaviour {

    [SerializeField] private GameObject characterController = null;
    [SerializeField] private GameObject KingelezCityRoot = null;
    [SerializeField] private GameObject KingelezCityExteriorRoot = null;
    [SerializeField] private GameObject startCityPosition = null;
    [SerializeField] private GameObject startPosition = null;
    [SerializeField] private GameObject endPosition = null;
    [SerializeField] private float macroScale = 10.0f;
    [SerializeField] private float microScale = 0.1f;
    [SerializeField] private float level0ShadowDistance = 50.0f;
    [SerializeField] private float level1ShadowDistance = 35.0f;
    [SerializeField] private float level2ShadowDistance = 25.0f;
    [SerializeField] private float level3ShadowDistance = 10.0f;
    public float maxDistanceLevel0 = 50.0f;
    public float maxDistanceLevel1 = 25.0f;
    public float maxDistanceLevel2 = 17.5f;
    public float maxDistanceLevel3 = 6.0f;

    public float[] levelTime = new float[4] { 35.0f, 35.0f, 25.0f, 60.0f};

    [HideInInspector]
    public float currentTimer = 0.0f;
    int currentMarkerLevel = 0;

    [HideInInspector] // Hides var below
    public GameObject activeMarker = null;           // active marker to hide

    public float markerScaleTimeOffset = 1.0f;
    public float markerScaleDuration = 3.0f;

    bool hasStarted = false;
    bool hasEnded = false;

    CameraScript cameraScript_ = null;

    public Material skyboxWithoutGround = null;
    public Material skyboxWithGround = null;

    public StartPlateScript startPlateScript = null;
    public GameObject startPlate = null;
    public GameObject AllianzStartPlate = null;
    public GameObject AllianzStartPlate2 = null;
    public GameObject AllianzEndPlate = null;
    public GameObject endPlate = null;

    public PlayableDirector BeginTimeline = null;
    public PlayableDirector EndTimeline = null;

    bool overrideTeleport = false;
    bool writeRotation = false;
    Quaternion newRotation = Quaternion.identity;

    [HideInInspector]
    public bool disableLookAts = false;

    CameraScript CameraScript()
    {
        if (cameraScript_ == null)
            cameraScript_ = FindObjectOfType<CameraScript>();

        return cameraScript_;
    }

    void Init(Vector3 endPositionVector, Quaternion endRotation)
    {
        //teleport to end position
        if (characterController)
        {
            characterController.transform.position = endPositionVector;
            Vector3 headRotation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.Head).eulerAngles;
            characterController.transform.localRotation = Quaternion.Euler(endRotation.eulerAngles + new Vector3(0, -headRotation.y - 180.0f, 0)); //Quaternion.Euler(0.0f, -180.0f, 0.0f);
            characterController.transform.localScale = new Vector3(macroScale, macroScale, macroScale);
        }

        QualitySettings.shadowDistance = level0ShadowDistance;
        CameraScript().rotationY = 0.0f;

        KingelezCityRoot.SetActive(true);
        KingelezCityExteriorRoot.SetActive(false);

        //show end plate
        if (AllianzEndPlate != null)
        {
            AllianzEndPlate.SetActive(true);
        }
        hasEnded = true;
        //EndTimeline.Play();

        MarkerScript.InitAllMarkers();

        RenderSettings.fog = false;
        RenderSettings.skybox = skyboxWithoutGround;
    }

    void Restart(Vector3 startPositionVector)
    {
        //teleport to start position
        if (characterController)
        {
            characterController.transform.position = startPositionVector;
            characterController.transform.localRotation = Quaternion.Euler(0.0f, -180.0f, 0.0f);
            characterController.transform.localScale = new Vector3(macroScale, macroScale, macroScale);
        }

        currentMarkerLevel = 0;
        currentTimer = 1000000;

        hasEnded = false;
        EndTimeline.Stop();

        hasStarted = false;
        BeginTimeline.Stop();

        KingelezCityRoot.SetActive(false);
        KingelezCityExteriorRoot.SetActive(false);

        //hide all plates
        endPlate.SetActive(false);
        //AllianzStartPlate.SetActive(false);
        AllianzStartPlate2.SetActive(false);
        AllianzEndPlate.SetActive(false);

        //show start plate
        if (startPlate != null)
        {
            startPlate.SetActive(true);
        }

        if (startPlateScript != null)
        {
            startPlateScript.gameObject.SetActive(true);
            startPlateScript.enabled = true;
            startPlateScript.InitScale();
        }
    }

    // Use this for initialization
    void Start ()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        QualitySettings.shadowDistance = level0ShadowDistance;

        Init(endPosition.transform.position, endPosition.transform.localRotation);
        Restart(startPosition.transform.position);

        //TeleportCharacterControllerToKingelezCity();
    }

    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Application.Quit();
        }

        //reset level
        if (Input.GetKey(KeyCode.R))
        {
            currentMarkerLevel = 5;

            //display end plate
            currentTimer = 10000000;
            StartCoroutine(CameraScript().m_VRCameraFade.FadeWithAction(2.0f, new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), RestartKingelezCity, null));
            return;
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentTimer = -1.0f;
        }

        //manage sponsors
        if (currentMarkerLevel == 0 && hasStarted)
        {
            BeginTimeline.Play();
            hasStarted = false;

            //float sponsorTimer = levelTime[currentMarkerLevel] - currentTimer;

            //BeginTimeline.time = sponsorTimer;

 /*           if (sponsorTimer < 5.0f)
            {
                AllianzStartPlate.SetActive(true);
            }
            else if (sponsorTimer < 6.0f)
            {
                AllianzStartPlate.SetActive(false);
            }
            else if (sponsorTimer < 10.0f)
            {
                AllianzStartPlate2.SetActive(true);
            }
            else
            {
                AllianzStartPlate2.SetActive(false);
            }
*/
        }

        if (currentMarkerLevel == 4 && hasEnded )
        {
            //float sponsorTimer = levelTime[currentMarkerLevel] - currentTimer;

            //EndTimeline.time = sponsorTimer;

            EndTimeline.Play();
            hasEnded = false;

/*            if (sponsorTimer < 6.0f)
            {
                AllianzEndPlate.SetActive(true);
            }
            else if (sponsorTimer < 7.0f)
            {
                AllianzEndPlate.SetActive(false);
            }
            else if (sponsorTimer < 8.0f)
            {
                endPlate.SetActive(true);
            }*/
        }

        //       if(UnityEngine.XR.XRSettings.isDeviceActive)
        currentTimer -= Time.deltaTime;

        if ( currentTimer < 0.0f )
        {
            //omitting level 1 due to last moment changes
            if (currentMarkerLevel == 0)
            {
                currentMarkerLevel = 2;

                //AllianzStartPlate.SetActive(false);
                AllianzStartPlate2.SetActive(false);
            }
            else
                currentMarkerLevel++;

            if (currentMarkerLevel == 4)
            {
                currentTimer = levelTime[currentMarkerLevel];

                //display end plate
                StartCoroutine(CameraScript().m_VRCameraFade.FadeWithAction(2.0f, new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), DisplayEndPlate, null));
            }
            else if (currentMarkerLevel == 5)
            {
                currentTimer = 10000000;
                StartCoroutine(CameraScript().m_VRCameraFade.FadeWithAction(2.0f, new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), RestartScene, null));
            }
            else
            {
                if (currentMarkerLevel == 2)
                {
                    overrideTeleport = true;
                }

                currentTimer = levelTime[currentMarkerLevel];

                disableLookAts = true;
                StartCoroutine(CameraScript().m_VRCameraFade.FadeWithAction(2.0f, new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), ProcessObject, null));
            }
        }
    }

    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void DisplayEndPlate(GameObject go)
    {
        Init(endPosition.transform.position, endPosition.transform.localRotation);
    }

    void RestartScene(GameObject go)
    {
        Restart(startPosition.transform.position);
    }

    void RestartKingelezCity(GameObject go)
    {
        Init(endPosition.transform.position, endPosition.transform.localRotation);
        Restart(startPosition.transform.position);
    }

    void ProcessObject(GameObject go)
    {
        disableLookAts = false;

        Vector3 startPos = characterController.transform.position;

        if (overrideTeleport)
        {
            startPos = new Vector3(-4.276f, 14.500f, -23.279f);
        }

        if (currentMarkerLevel == 2)
        {
            RenderSettings.fog = true;
            RenderSettings.skybox = skyboxWithGround;
            KingelezCityExteriorRoot.SetActive(true);
        }

        GameObject destinationMarker = MarkerScript.FindClosestMarker(startPos, currentMarkerLevel);
        Debug.Log("markerLevel: "+ currentMarkerLevel);

        if (destinationMarker != null)
        {
            Vector3 rayStart = destinationMarker.transform.position;

            if (overrideTeleport)
            {
                writeRotation = true;
                newRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                rayStart = startPos;

                overrideTeleport = false;
            }
            Ray ray = new Ray(rayStart, Vector3.down);
            RaycastHit hit;
            int layerMask = 1 << 2; //ignore raycast

            bool raycastDown = currentMarkerLevel == 0 ? false : true;

            Vector3 hitPoint = transform.position;

            if (raycastDown && Physics.Raycast(ray, out hit, 100.0f, ~layerMask))
            {
                hitPoint = hit.point;
            }
            else
            {
                hitPoint.y = 0.0f;
            }
            TeleportCharacterController(destinationMarker, hitPoint);
        }
    }

    public void TeleportCharacterController(GameObject targetMarker, Vector3 hitPoint)
    {
        MarkerScript marker = targetMarker.GetComponent<MarkerScript>();

        float scale = 1.0f;

        if (marker)
        {
            if (marker.markerLevel == 1) //top buildings
            {
                scale = microScale;
                QualitySettings.shadowDistance = level1ShadowDistance;
                //AkSoundEngine.PostEvent("cityVoice2", gameObject);
            }
            else if (marker.markerLevel == 2) //top buildings
            {
                scale = microScale;
                QualitySettings.shadowDistance = level2ShadowDistance;
                //AkSoundEngine.PostEvent("cityVoice3", gameObject);
            }
            else if (marker.markerLevel == 3) //top buildings
            {
                scale = microScale;
                QualitySettings.shadowDistance = level3ShadowDistance;
                //AkSoundEngine.PostEvent("cityVoice4", gameObject);
            }
            else  //macro scale
            {
                scale = macroScale;
                QualitySettings.shadowDistance = level0ShadowDistance;

                //play 1st voice
                //AkSoundEngine.PostEvent("cityVoice2", gameObject);
            }
        }
        else  //macro scale
        {
            scale = macroScale;
            QualitySettings.shadowDistance = level0ShadowDistance;

            //play 1st voice
            //AkSoundEngine.PostEvent("cityVoice2", gameObject);
        }

        Vector3 flatCameraOffset = Vector3.zero;

        if (!UnityEngine.XR.XRSettings.isDeviceActive)
            flatCameraOffset = Vector3.up * 1.7f * scale;

        Vector3 newPosition = hitPoint + flatCameraOffset;
        if (characterController)
        {
            characterController.transform.position = newPosition;
            if (writeRotation)
            {
                characterController.transform.localRotation = newRotation;
                writeRotation = false;
            }
//            else
//                characterController.transform.localRotation = Quaternion.Euler(0.0f, -180.0f, 0.0f);

            characterController.transform.localScale = new Vector3(scale, scale, scale);
        }

        if (activeMarker)
        {
            activeMarker.SetActive(true);
            if (activeMarker.GetComponent<MarkerScript>().occlusionCollision != null)
            {
                activeMarker.GetComponent<MarkerScript>().occlusionCollision.gameObject.SetActive(true);
            }
        }

        activeMarker = targetMarker;

        activeMarker.SetActive(false);
        if (marker.occlusionCollision != null)
        {
            marker.occlusionCollision.gameObject.SetActive(false);
        }

        StartCoroutine(MarkerScript.ScaleMarkers(marker, currentMarkerLevel, newPosition, markerScaleDuration, markerScaleTimeOffset, marker.markerLevel != 0));
    }

    public void TeleportCharacterControllerToKingelezCity()
    {
        Vector3 startPositionVector = startCityPosition.transform.position;

        //teleport to start position
        if (characterController)
        {
            characterController.transform.position = startPositionVector;
            characterController.transform.localRotation = Quaternion.Euler(0.0f, -180.0f, 0.0f);
        }

        if (startPlate != null)
        {
            startPlate.SetActive(false);
        }

        //AllianzStartPlate.SetActive(true);
        hasStarted = true;
        //BeginTimeline.Play();

        //markers appear

        if (KingelezCityRoot != null)
            KingelezCityRoot.SetActive(true);

        //AkSoundEngine.PostEvent("cityVoice1", gameObject);

        currentTimer = levelTime[0];

        StartCoroutine(MarkerScript.ScaleMarkers( null, 0, startPositionVector, markerScaleDuration, markerScaleTimeOffset, false));
    }
}
