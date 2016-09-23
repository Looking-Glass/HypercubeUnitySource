using UnityEngine;
using System.Collections;

namespace hypercube
{
    public class basicSettingsAdjustor : MonoBehaviour {

    public UnityEngine.UI.InputField modelName;
    public UnityEngine.UI.InputField versionNumber;

    public UnityEngine.UI.InputField resX;
    public UnityEngine.UI.InputField resY;

    public UnityEngine.UI.InputField sliceCount;

    public castMesh canvas;


     void OnEnable()
    {
        if (!canvas)
            return;

        dataFileDict d = canvas.GetComponent<dataFileDict>();

        modelName.text = d.getValue("volumeModelName", "UNKNOWN!");
        versionNumber.text = d.getValue("volumeHardwareVersion", "-9999");

        resX.text = d.getValueAsInt("volumeResX", 1920).ToString();
        resY.text = d.getValueAsInt("volumeResY", 1080).ToString();

        sliceCount.text = canvas.slices.ToString();
    }

    void OnDisable()
    {
        if (!canvas)
            return;

        dataFileDict d = canvas.GetComponent<dataFileDict>();

        d.setValue("volumeModelName", modelName.text);
        d.setValue("volumeHardwareVersion", dataFileDict.stringToFloat(versionNumber.text, -9999f));

        d.setValue("volumeResX", dataFileDict.stringToInt(resX.text, 1920));
        d.setValue("volumeResY", dataFileDict.stringToInt(resY.text, 1080));

        canvas.slices = dataFileDict.stringToInt(sliceCount.text, 10);

        //set the res, if it is different.
        int resXpref =  d.getValueAsInt("volumeResX", 1920);
        int resYpref = d.getValueAsInt("volumeResY", 1080);
        bool forceFullScreen = true;
#if UNITY_EDITOR
        forceFullScreen = false;
#endif
        if (Screen.width != resXpref || Screen.height != resYpref)
            Screen.SetResolution(resXpref, resYpref, forceFullScreen);
    }
}
}


