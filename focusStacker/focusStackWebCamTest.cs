// Sets the device of the WebCamTexture to the first one available and starts playing it
using UnityEngine;
using System.Collections;

public class focusStackWebCamTest : MonoBehaviour
{
    public focusStackedEffect focusStack;
    public RenderTexture renderTarget;

    WebCamTexture webcamTexture = null;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture();

        if (devices.Length > 0)
        {
            webcamTexture.deviceName = devices[0].name;
            webcamTexture.Play();
        }
        else
        {
            Debug.LogWarning("Didn't find any webcams.");
            enabled = false;
        }
    }

    void Update()
    {
        if (webcamTexture && webcamTexture.didUpdateThisFrame)
        {
            focusStack.processFrame(webcamTexture, renderTarget);
        }
        
    }
}