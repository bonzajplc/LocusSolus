using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VR = UnityEngine.VR;
using VRStandardAssets.Utils;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class CameraScript : MonoBehaviour {

    [SerializeField] private int m_ExclusionLayers = 1 << 13;     // "MarkerOcclusionCollision" layer to exclude from the raycast.

    [SerializeField] private bool showDebugRay = false;           // Optionally show the debug ray.
    [SerializeField] private float debugRayLength = 5f;           // Debug ray length.
    [SerializeField] private float debugRayDuration = 1f;         // How long the Debug ray will remain visible.
    [SerializeField] private float rayLength = 500f;              // How far into the scene the ray is cast.
    [SerializeField] private Reticle m_Reticle = null;

    [HideInInspector] // Hides var below
    public GameObject visibleObject = null;           // active marker to hide

    public float horizontalMouseSpeed = 2.0F;
	public float verticalMouseSpeed = 2.0F;

    public FadeController m_VRCameraFade = null;

    /// <summary>
    /// If true, dynamic resolution will be enabled
    /// </summary>
    public bool enableAdaptiveResolution = false;

    [RangeAttribute(0.5f, 2.0f)]
    public float renderScaleQuality0 = 0.75f;
    public float renderScaleQuality1 = 1.00f;
    public float renderScaleQuality2 = 1.10f;
    public float renderScaleQuality3 = 1.25f;
    /// <summary>
    /// Max RenderScale the app can reach under adaptive resolution mode ( enableAdaptiveResolution = ture );
    /// </summary>
    [RangeAttribute(0.5f, 2.0f)]
    public float maxRenderScale = 1.0f;

    /// <summary>
    /// Min RenderScale the app can reach under adaptive resolution mode ( enableAdaptiveResolution = ture );
    /// </summary>
    [RangeAttribute(0.5f, 2.0f)]
    public float minRenderScale = 0.7f;

    private static bool _isUserPresentCached = false;
    private static bool _isUserPresent = false;
    
    private static float _gpuScale = 1.0f;

    GameObject characterController_ = null;

    GameObject CharacterController()
    {
        if (characterController_ == null)
            characterController_ = transform.parent.parent.gameObject;

        return characterController_;
    }

    /// <summary>
    /// True if the user is currently wearing the display.
    /// </summary>
    public bool IsUserPresent
    {
        get
        {
            if (!_isUserPresentCached)
            {
                _isUserPresentCached = true;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                _isUserPresent = OVRPlugin.userPresent;
#else
				_isUserPresent = false;
#endif
            }

            return _isUserPresent;
        }

        private set
        {
            _isUserPresentCached = true;
            _isUserPresent = value;
        }
    }

    float mainSpeed = 5.0f; //regular speed
    float shiftAdd = 15.0f; //multiplied by how long shift is held.  Basically running
    float maxShift = 40.0f; //Maximum speed when holdin gshift
    float camSens = 0.15f; //How sensitive it with mouse
    private float totalRun = 1.0f;
    public float rotationY = 0.0f;

    void UpdateMouseCamera()
    {
       if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float inputLeftRight = Input.GetAxis("Mouse X") * horizontalMouseSpeed;
        float inputUpDown = -Input.GetAxis("Mouse Y") * verticalMouseSpeed;

        rotationY += inputUpDown;
        rotationY = ClampAngle(rotationY, -82, 82);

        transform.eulerAngles = new Vector3(rotationY, transform.eulerAngles.y + inputLeftRight, 0);

        //Keyboard commands
        Vector3 p = GetBaseInput();

        if (Input.GetKey(KeyCode.LeftShift))
        {
            totalRun += Time.deltaTime;
            p = p * totalRun * shiftAdd;
            p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
            p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
            p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
        }
        else
        {
            totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
            p = p * mainSpeed;
        }

        p = p * Time.deltaTime;

        Matrix4x4 trans = Matrix4x4.Rotate(transform.rotation) * Matrix4x4.Translate(p);
        CharacterController().transform.position += trans.MultiplyPoint(Vector3.zero);
    }

    public float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360.0f)
            angle += 360.0f;
        if (angle > 360.0f)
            angle -= 360.0f;
        return Mathf.Clamp(angle, min, max);
    }

    private Vector3 GetBaseInput()
    { //returns the basic values, if it's 0 than it's not active.
        Vector3 p_Velocity = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            p_Velocity += new Vector3(0, 0, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            p_Velocity += new Vector3(0, 0, -1);
        }
        if (Input.GetKey(KeyCode.A))
        {
            p_Velocity += new Vector3(-1, 0, 0);
        }
        if (Input.GetKey(KeyCode.D))
        {
            p_Velocity += new Vector3(1, 0, 0);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            p_Velocity += new Vector3(0, -1, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            p_Velocity += new Vector3(0, 1, 0);
        }
        return p_Velocity;
    }

void Awake () {
#if UNITY_ANDROID
        GetComponent<PostProcessLayer>().enabled = false;
        renderScale = 0.75f;
#endif
        // By default (for PC) it's [0] for Low and [3] for Ultra quality.
        float renderScaleQuality = 0.0f;
        if (QualitySettings.GetQualityLevel() == 0)
        {
            renderScaleQuality = renderScaleQuality0;
        }
        else if (QualitySettings.GetQualityLevel() == 1)
        {
            renderScaleQuality = renderScaleQuality1;
        }
        else if (QualitySettings.GetQualityLevel() == 2)
        {
            renderScaleQuality = renderScaleQuality2;
        }
        else if (QualitySettings.GetQualityLevel() == 3)
        {
            renderScaleQuality = renderScaleQuality3;
        }
        else
        {
            renderScaleQuality = 1.0f;
        }

        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = renderScaleQuality;
        if (UnityEngine.XR.XRSettings.loadedDeviceName == "Oculus")
            transform.parent.localPosition  =   new Vector3(0.0f, OVRPlugin.eyeHeight, 0.0f);
        else
            transform.parent.localPosition  =   Vector3.zero;
    }

    private void Start()
    {
        //if (!showReticle)
        //    m_Reticle.Hide();
    }

    // Update is called once per frame
    void Update ()
    {
        if (!UnityEngine.XR.XRSettings.isDeviceActive)
            UpdateMouseCamera();

        // Show the debug ray if required
        if (showDebugRay)
        {
            Debug.DrawRay(transform.position, transform.forward * debugRayLength, Color.blue, debugRayDuration);
        }

        // Create a ray that points forwards from the camera.
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

		visibleObject = null;

        // Do the spherecast forward to see if we hit an interactive item
        if ( Physics.Raycast( ray, out hit, rayLength, ~m_ExclusionLayers ) )
        {
            if (m_Reticle)
                m_Reticle.SetPosition(hit);

            LookAtObject lookAtObject = hit.transform.GetComponent<LookAtObject>();

            if (lookAtObject != null)
            {
                visibleObject = hit.transform.gameObject;
                lookAtObject.ProcessHit();
            }
        }
        else
        {
            // Position the reticle at default distance.
			if (m_Reticle)
				m_Reticle.SetPosition();
        }

        //adaptive resolution
        if (enableAdaptiveResolution)
        {
            float scalingFactor = GetPerformanceScaleFactor();

            if (UnityEngine.XR.XRSettings.eyeTextureResolutionScale < maxRenderScale)
            {
                // Allocate renderScale to max to avoid re-allocation
                UnityEngine.XR.XRSettings.eyeTextureResolutionScale = maxRenderScale;
            }
            else
            {
                // Adjusting maxRenderScale in case app started with a larger renderScale value
                maxRenderScale = Mathf.Max(maxRenderScale, UnityEngine.XR.XRSettings.eyeTextureResolutionScale);
            }

            scalingFactor = Mathf.Clamp(scalingFactor, minRenderScale, maxRenderScale);
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = scalingFactor;
            //VR.VRSettings.renderViewportScale = scalingFactor;

            //Debug.Log("scaleFactor: " + scalingFactor);
        }
        

    }

    float GetPerformanceScaleFactor()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        OVRPlugin.AppPerfStats stats = OVRPlugin.GetAppPerfStats();
        float scale = Mathf.Sqrt(stats.AdaptiveGpuPerformanceScale);
#else
		float scale = 1.0f;
#endif
        _gpuScale = Mathf.Clamp(_gpuScale * scale, 0.5f, 2.0f);
        return _gpuScale;
    }
}
