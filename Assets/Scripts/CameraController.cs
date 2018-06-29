using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public float mainSpeed = 5.0f; //regular speed
    public float shiftAdd = 15.0f; //multiplied by how long shift is held.  Basically running
    public float maxShift = 40.0f; //Maximum speed when holdin gshift
    public float mouseSensitivity = 0.15f; //How sensitive it with mouse
    public float padSensitivity = 1.0f; //How sensitive it with mouse
    private float totalRun = 1.0f;
    float rotationY = 0.0f;

    [HideInInspector]
    public bool lockedRotation = false;

    // Use this for initialization
    void Start () {
		
	}

    void Update()
    {
        //if (Cursor.lockState != CursorLockMode.Locked)
         //   return;

        float inputLeftRight = Input.GetAxis("Mouse X") * mouseSensitivity;
        float inputUpDown = Input.GetAxis("Mouse Y") * mouseSensitivity * -1;

        if( !lockedRotation )
        {
            rotationY += inputUpDown;
            rotationY = ClampAngle(rotationY, -82, 82);

            transform.eulerAngles = new Vector3(rotationY, transform.eulerAngles.y + inputLeftRight, 0);
        }

        lockedRotation = false;

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
        transform.position += trans.MultiplyPoint(Vector3.zero);
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
    { 
        //returns the basic values, if it's 0 than it's not active.
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
}
