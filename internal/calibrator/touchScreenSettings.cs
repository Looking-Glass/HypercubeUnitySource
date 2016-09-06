using UnityEngine;
using System.Collections;

namespace hypercube
{
    public class touchScreenSettings : touchScreenTarget
    {
        public UnityEngine.UI.InputField resXInput;
        public UnityEngine.UI.InputField resYInput;
        public UnityEngine.UI.InputField sizeWInput;
        public UnityEngine.UI.InputField sizeHInput;
        public UnityEngine.UI.InputField sizeDInput;

        public UnityEngine.UI.Text display;

        public hypercubeCamera cam;

        protected override void OnEnable()
        {
            dataFileDict d = cam.localCastMesh.gameObject.GetComponent<dataFileDict>();

            resXInput.text = d.getValue("touchScreenResX", "800");
            resYInput.text = d.getValue("touchScreenResY", "480");
            sizeWInput.text = d.getValue("projectionCentimeterWidth", "20");
            sizeHInput.text = d.getValue("projectionCentimeterHeight", "12");
            sizeDInput.text = d.getValue("projectionCentimeterDepth", "20");

            base.OnEnable();
        }


        protected override void OnDisable()
        {
            dataFileDict d = cam.localCastMesh.gameObject.GetComponent<dataFileDict>();

            d.setValue("touchScreenResX", resXInput.text);
            d.setValue("touchScreenResY", resYInput.text);
            d.setValue("projectionCentimeterWidth", sizeWInput.text);
            d.setValue("projectionCentimeterHeight", sizeHInput.text);
            d.setValue("projectionCentimeterDepth", sizeDInput.text);

            base.OnDisable();
        }

        public override void onTouchMoved(touch touch)
        {
            touchInterface i = new touchInterface();
            touch._getInterface(ref i);

            display.text = "Latest Values:\nx: " + i.rawTouchScreenX + "\ny: " + i.rawTouchScreenY;
        }
    }
}