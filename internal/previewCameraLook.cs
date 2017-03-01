using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//if we don't have settings and can't detect Volume, we can use this to display the preview to a user.

public class previewCameraLook : MonoBehaviour {

    public Transform cam;
    public float sensitivityX = 30f;
    public float sensitivityY = 30f;
    float rotationX = 0f;
    float rotationY = 0f;
	
	// Update is called once per frame
	void Update () 
    {
        float xLook = 0f;
        float yLook = 0f;

        xLook = Input.mousePosition.x;
        yLook = Input.mousePosition.y;

        if (xLook != 0 || yLook != 0 )
        {
            rotationX += xLook * sensitivityX * Time.deltaTime;
            rotationY += yLook * sensitivityY * Time.deltaTime;
            //rotationX = ClampAngle(rotationX, minimumX, maximumX);
            //rotationY = ClampAngle(rotationY, minimumY, maximumY);


            //Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            //Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);
            //Quaternion zQuaternion = Quaternion.AngleAxis(0f, Vector3.forward);
            //cam.localRotation = xQuaternion * yQuaternion * zQuaternion;
            cam.Rotate(0f, xLook * sensitivityX * Time.deltaTime, 0f);
        }
		
	}
}
