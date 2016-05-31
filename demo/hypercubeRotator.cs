using UnityEngine;
using System.Collections;

public class hypercubeRotator : MonoBehaviour {

    public float pauseTime = 4f;
    float paused = -1f;

    public GameObject rotated;
    public float rotationSpeed;
    public float scaleSpeed;
    public float scaleMod;
    public float verticalSwingSpeed;
    public float verticalSwing;   

    Vector3 startScale;
    Vector3 startRot;
    Vector3 currentRot;
    float startRotateTime;
    void Start()
    {
        reset();
    }
	
	// Update is called once per frame
	void Update () 
    {
        if ( 
            Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) ||
            Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.E) ||
            Input.GetKey(KeyCode.Q) ||
            Input.GetKey(KeyCode.R) ||
            Input.GetKey(KeyCode.Tab) ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetAxis("Mouse X") != 0 ||
            Input.GetAxis("Mouse Y") != 0
            )
        {
            paused = pauseTime;
            return;
        }

        if (paused > 0)
        {
            paused -= Time.deltaTime;
            if (paused <= 0)
                reset();
            else
                return;
        }

        //auto rotation
        currentRot = startRot;
        float timeDiff = Time.timeSinceLevelLoad - startRotateTime;
        currentRot.y += rotationSpeed * timeDiff;
        currentRot.x += Mathf.Sin(timeDiff * verticalSwingSpeed) * verticalSwing;
        rotated.transform.localRotation = Quaternion.Euler(currentRot);

        //scale
        Vector3 temp = startScale;
        float mod = Mathf.Sin(timeDiff * scaleSpeed) * scaleMod;
        if (mod < .01f)
            mod = .01f;
        temp.x += mod;
        temp.y += mod;
        temp.z += mod;
        rotated.transform.localScale = temp;      
	}

    void reset()
    {
        startScale = rotated.transform.localScale;
        startRot = rotated.transform.localRotation.eulerAngles;
        startRotateTime = Time.timeSinceLevelLoad;
    }
}
