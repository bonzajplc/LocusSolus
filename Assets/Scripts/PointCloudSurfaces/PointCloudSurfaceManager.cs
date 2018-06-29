using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;


public class PointCloudSurfaceManager : MonoBehaviour
{
    // Native plugin rendering events are only called if a plugin is used
    // by some script. This means we have to DllImport at least
    // one function in some active script.
    // For this example, we'll call into plugin's SetTimeFromUnity
    // function and pass the current time so the plugin can animate.
    [DllImport("RenderingPlugin")]
    private static extern int InitializeKinectFromUnity();

    [DllImport ("RenderingPlugin")]
	private static extern void SetTimeFromUnity(float t);

    [DllImport("RenderingPlugin")]
    private static extern IntPtr GetRenderEventFunc();

    public bool KinectPreview = true;
    static int kinectInitialized = 0;

    void Start()
	{
        if( KinectPreview )
        {
            if (kinectInitialized == 0)
                kinectInitialized = InitializeKinectFromUnity();
            else
                Debug.LogError("unable to initialize Kinect v2.0!");
        }

        StartCoroutine("CallPluginAtEndOfFrames");
	}

    private void Update()
    {
    }

    private IEnumerator CallPluginAtEndOfFrames()
	{
		while (true) {
			// Wait until all frame rendering is done
			yield return new WaitForEndOfFrame();

			// Set time for the plugin
			SetTimeFromUnity (Time.timeSinceLevelLoad);

            // Issue a plugin event with arbitrary integer identifier.
            // The plugin can distinguish between different
            // things it needs to do based on this ID.
            // For our simple plugin, it does not matter which ID we pass here.
            GL.IssuePluginEvent(GetRenderEventFunc(), 1);
        }
    }
}
