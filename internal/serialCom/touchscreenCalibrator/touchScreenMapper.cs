using UnityEngine;
using System.Collections;

public class touchScreenMapper : touchscreenTarget {

    public TextMesh outputText;
    public GameObject arrow;
    public GameObject circle;

    public hypercubeCamera cam;

    public GameObject guiCanvas;
    public UnityEngine.UI.InputField resXInput;
    public UnityEngine.UI.InputField resYInput;
    public UnityEngine.UI.InputField sizeWInput;
    public UnityEngine.UI.InputField sizeHInput;
    public UnityEngine.UI.InputField sizeDInput;

    enum calibrationStage
    {
        STEP_INVALID = -1,
        STEP_calibrate = 0,
        STEP_settings,
        STEP_touchCorner1,
        STEP_touchCorner2,
        STEP_touchCorner3,
        STEP_touchCorner4,
        STEP_save
    }

    calibrationStage stage;

    int ULx = 0;
    int ULy = 0;
    int URx = 0;
    int URy = 0;
    int LRx = 0;
    int LRy = 0;
    int LLx= 0;
    int LLy = 0;

	// Use this for initialization
	void Awake () 
    {
        stage = calibrationStage.STEP_INVALID;
	}
	
	// Update is called once per frame
	void Update () 
    {
        if (stage == calibrationStage.STEP_INVALID) //do this here to ensure that our datafiledict and all regular hypercube stuff has time to load
            goToNextStage();

        if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftShift)) //quit
        {
            if (stage == calibrationStage.STEP_save)
                save();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Return)) //go to next stage
        {
            goToNextStage();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) //go to next stage
        {
            quit();
            return;
        }



        if (stage == calibrationStage.STEP_save)
        {
            if (hypercube.input.frontScreen.touchCount > 0)
            {
                //debug info
                //hypercube.touchInterface i = new hypercube.touchInterface();
                //hypercube.input.frontScreen.touches[0]._getInterface(ref i);
                //outputText.text = hypercube.input.frontScreen.touches[0].id + ":  " + hypercube.input.frontScreen.touches[0].posX + " - " + hypercube.input.frontScreen.touches[0].posY + "\n" + i.rawTouchScreenX + "  " + i.rawTouchScreenY;
                circle.transform.position = hypercube.input.frontScreen.touches[0].getWorldPos(cam);
            }
        }	
	}

    void goToNextStage()
    {
        if (stage == calibrationStage.STEP_INVALID) //first time through. try to put decent defaults.
        {
            dataFileDict d = cam.localCastMesh.gameObject.GetComponent<dataFileDict>();
            resXInput.text = d.getValue("touchScreenResX", "800");
            resYInput.text = d.getValue("touchScreenResY", "480");
            sizeWInput.text = d.getValue("projectionCentimeterWidth", "20");
            sizeHInput.text = d.getValue("projectionCentimeterHeight", "12");
            sizeDInput.text = d.getValue("projectionCentimeterDepth", "20");
        }
 
        stage++;

        if (stage > calibrationStage.STEP_save)
            stage = calibrationStage.STEP_calibrate;


        if (stage == calibrationStage.STEP_calibrate)
        {
            guiCanvas.SetActive(false);
            arrow.SetActive(false);
            circle.SetActive(false);
            outputText.text = "To configure the touch screen, Volume must be calibrated first.\nIt should not have any distortions.\nIf it needs calibration, do that first.\nIf Volume is nice and rectangular, press ENTER to continue.";
        }
        else if (stage == calibrationStage.STEP_settings)
        {
            guiCanvas.SetActive(true);
        }
        else if (stage == calibrationStage.STEP_touchCorner1)
        {
            guiCanvas.SetActive(false);
            arrow.SetActive(true);
            outputText.text = "\n\n\nAlign your finger to the arrow corner.\nThen lift your finger.";
            arrow.transform.localRotation = Quaternion.identity;
            arrow.transform.localPosition = cam.transform.TransformPoint(-.5f, .5f, -.5f);
        }
        else if (stage == calibrationStage.STEP_touchCorner2)
        {
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            arrow.transform.localPosition = cam.transform.TransformPoint(.5f, .5f, -.5f);
        }
        else if (stage == calibrationStage.STEP_touchCorner3)
        {
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            arrow.transform.localPosition = cam.transform.TransformPoint(.5f, -.5f, -.5f);
        }
        else if (stage == calibrationStage.STEP_touchCorner4)
        {
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            arrow.transform.localPosition = cam.transform.TransformPoint(-.5f, -.5f, -.5f);
        }
        else if (stage == calibrationStage.STEP_save)
        {
            arrow.SetActive(false);
            circle.SetActive(true);
            outputText.text = "\nMake sure that the circle is aligned with your finger.\nIf it is, press Lshift + S to save.\nOtherwise press ENTER to try again.";
        }
    }

    public override void onTouchUp(hypercube.touch touch)
    {
        hypercube.touchInterface i = new hypercube.touchInterface();
        touch._getInterface(ref i);
         if (stage == calibrationStage.STEP_touchCorner1)
        {
            ULx = i.rawTouchScreenX;
            ULy = i.rawTouchScreenY;
            goToNextStage();
        }
        else if (stage == calibrationStage.STEP_touchCorner2)
        {
            URx = i.rawTouchScreenX;
            URy = i.rawTouchScreenY;
            goToNextStage();
        }
        else if (stage == calibrationStage.STEP_touchCorner3)
        {
            LRx = i.rawTouchScreenX;
            LRy = i.rawTouchScreenY;
            goToNextStage();
        }
        else if (stage == calibrationStage.STEP_touchCorner4)
        {
            LLx = i.rawTouchScreenX;
            LLy = i.rawTouchScreenY;
            set();
            goToNextStage();
        }

         
    }

    void set()
    {
        //save the settings...
        dataFileDict d = cam.localCastMesh.gameObject.GetComponent<dataFileDict>();
        d.setValue("touchScreenResX", resXInput.text);
        d.setValue("touchScreenResY", resYInput.text);
        d.setValue("projectionCentimeterWidth", sizeWInput.text);
        d.setValue("projectionCentimeterHeight", sizeHInput.text);
        d.setValue("projectionCentimeterDepth", sizeDInput.text);

        //gather normalized limits
        float resX = d.getValueAsFloat("touchScreenResX", 800f);
        float resY = d.getValueAsFloat("touchScreenResY", 480f);

        float top = (float)(ULy + URy) / 2f;//use averages.
        float bottom = (float)(LLy + LRy) / 2f;
        float left = (float)(ULx + LLx) / 2f;
        float right = (float)(URx + LRx) / 2f;

        top /= resY; //normalize the raw averages
        bottom /= resY;
        left /= resX;
        right /= resX;

        d.setValue("touchScreenMapTop", top); 
        d.setValue("touchScreenMapBottom", bottom);
        d.setValue("touchScreenMapLeft", left);
        d.setValue("touchScreenMapRight", right);

        hypercube.input.frontScreen.setTouchScreenDims(d);
    }

    void save()
    {
        dataFileDict d = cam.localCastMesh.gameObject.GetComponent<dataFileDict>();
        d.save();
        outputText.text = "\n\n\nSAVED!\n\nPress ESCAPE to exit.";
    }
    void quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

}
