using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class LocusSolus_teaser : MonoBehaviour {

    GameObject[] cubes = null;
    Quaternion[] cubeRotations = null;
    public float rotationScale = 1.0f;

    GameObject[] spheres = null;
    Vector3[] sphereBounces = null;
    Vector3[] spherePositions = null;
    public float bounceScale = 1.0f;
    public Vector3 HandOffset;

    DensityFieldInstancer[] instances = null;
    Quaternion[] instanceRotations = null;

    public Transform CharacterController = null;
    public DensityObject Head = null;
    public Transform HandL = null;
    public Transform HandR = null;
    public PlayableDirector timeline = null;

    public Image image;

    public float AttachmentStrength = 1000.0f;

    public float isoLevel = 1.0f;
    public float isoScale = 1.0f;

    public float acceleration = 0.1f;

    public WireframeMarchingCubesRenderer wireframeRenderer = null;
    public DensityFieldManager densityFieldManager= null;

    Quaternion lastRot = Quaternion.identity;

    float wireAccumulator = 0.0f;
    bool updateTime = true;
    bool buttonReleased = true;

    TickController[] ticks = null;

    public float outputAxis = 0.0f;

    // Use this for initialization
    void Start () {
        //find all objects marked ass cube
        cubes = GameObject.FindGameObjectsWithTag("Cube");
        cubeRotations = new Quaternion[cubes.Length];

        for (int i = 0; i < cubes.Length; i++)
        {
            cubeRotations[i] = Quaternion.AngleAxis(rotationScale * Random.Range(-1.0f, 1.0f), Random.onUnitSphere);
        }

        spheres = GameObject.FindGameObjectsWithTag("Sphere");
        sphereBounces = new Vector3[spheres.Length];
        spherePositions = new Vector3[spheres.Length];

        for (int i = 0; i < spheres.Length; i++)
        {
            Vector3 v3 = bounceScale * Random.onUnitSphere;
            sphereBounces[i].x = v3.x;
            sphereBounces[i].y = v3.y;
            sphereBounces[i].z = v3.z;

            spherePositions[i] = spheres[i].transform.position;
        }

        //find all marching cubes instances
        instances = GameObject.FindObjectsOfType<DensityFieldInstancer>();
        instanceRotations = new Quaternion[cubes.Length];

        for (int i = 0; i < instances.Length; i++)
        {
            instanceRotations[i] = Quaternion.AngleAxis(rotationScale * Random.Range(-0.30f, 0.3f), Random.onUnitSphere);
        }

        ticks = GameObject.FindObjectsOfType<TickController>();
    }

    Vector3 tmp;

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < cubes.Length; i++)
        {
            cubes[i].transform.rotation *= cubeRotations[i];
        }

        for (int i = 0; i < spheres.Length; i++)
        {
            tmp = spherePositions[i];
            tmp.x += sphereBounces[i].x * Mathf.Sin(Time.timeSinceLevelLoad);
            tmp.y += sphereBounces[i].y * Mathf.Cos(Time.timeSinceLevelLoad);
            tmp.z += sphereBounces[i].z * Mathf.Sin(-Time.timeSinceLevelLoad);
            spheres[i].transform.position = tmp;
        }

        for (int i = 0; i < instances.Length; i++)
        {
            instances[i].transform.rotation *= instanceRotations[i];
        }

        if (UnityEngine.XR.XRSettings.enabled)
        {
            if (HandL)
            {
                //Vector3 LeftHandInWorldSpace = CharacterController.transform.TransformPoint(HandOffset + UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.LeftHand));
                HandL.localPosition = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.LeftHand);
                HandL.localRotation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.LeftHand);

                if (Input.GetAxis("Fire1") > 0.1f)
                {
                    if (buttonReleased)
                    {
                        updateTime = !updateTime;

                        if(updateTime)
                        {
                            image.gameObject.SetActive(false);

                            //turn on tick components
                            foreach ( var tick in ticks )
                            {
                                tick.gameObject.SetActive(true);
                            }
                        }
                        else
                        {
                            image.gameObject.SetActive(true);

                            //turn off tick components
                            foreach (var tick in ticks)
                            {
                                tick.gameObject.SetActive(false);
                            }
                        }
                    }
                    buttonReleased = false;
                }
                else
                    buttonReleased = true;

                //Vector3 currentScreenPosLeftHand = Camera.main.WorldToViewportPoint(HandL.position);
                if (Input.GetAxis("Horizontal") > 0.01f )
                {
                    //rewind left and right
                    var deltaRot = HandL.rotation * Quaternion.Inverse(lastRot);
                    var eulerRot = new Vector3(Mathf.DeltaAngle(0, deltaRot.eulerAngles.x), Mathf.DeltaAngle(0, deltaRot.eulerAngles.y), Mathf.DeltaAngle(0, deltaRot.eulerAngles.z));

                    timeline.time += 0.1f * eulerRot.y;
                }
                else
                {
                    if( updateTime )
                        timeline.time += Time.deltaTime;
                }

                if (timeline.time > timeline.duration)
                    timeline.time = 0.0f;

                if (image.gameObject.activeSelf)
                    image.fillAmount = (float)timeline.time / (float)timeline.duration;

                timeline.Evaluate();
                lastRot = HandL.rotation;
            }
            if (HandR)
            {
                HandR.localPosition = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.RightHand);
                HandR.localRotation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.RightHand);

                if (Input.GetAxis("Vertical") > 0.01f || Input.GetAxis("Fire2") > 0.01f)
                {
                    float input = Mathf.Max(Input.GetAxis("Vertical"), Input.GetAxis("Fire2"));

                    wireAccumulator += Time.deltaTime * input * ( 1.0f - wireAccumulator );
                }
                else
                    wireAccumulator -= Time.deltaTime;

                wireAccumulator = Mathf.Clamp01(wireAccumulator);

                outputAxis = Mathf.Sin(wireAccumulator * Mathf.PI / 2.0f);

                if (wireframeRenderer)
                {
                    float lineWidth = 0.5f * Mathf.SmoothStep(0.0f, 1.0f, wireAccumulator);
                    wireframeRenderer.lineWidth = lineWidth;
                    wireframeRenderer.isoLevel = 1.0f - 0.15f * Mathf.Sin( wireAccumulator * Mathf.PI / 2.0f );
                }
            }
            if (Head)
                Head.transform.localPosition = HandOffset + UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head);
        }
    }
}
