using UnityEngine;
using System.Collections;

namespace hypercube
{
    public class generalSettingsAdjustor : MonoBehaviour
    {

        float sensitivity = 1f;
        public KeyCode nudgeUp = KeyCode.W;
        public KeyCode nudgeDown = KeyCode.S;
        public KeyCode nudgeLeft = KeyCode.A;
        public KeyCode nudgeRight = KeyCode.D;
        public KeyCode sliceHeightUp = KeyCode.R;
        public KeyCode sliceHeightDown = KeyCode.F;
        public KeyCode sliceGapUp = KeyCode.T;
        public KeyCode sliceGapDown = KeyCode.G;
        public KeyCode sliceWidthUp = KeyCode.X;
        public KeyCode sliceWidthDown = KeyCode.Z;

        public UnityEngine.UI.InputField offsetX;
        public UnityEngine.UI.InputField offsetY;
        public UnityEngine.UI.InputField sliceW;
        public UnityEngine.UI.InputField sliceH;
        public UnityEngine.UI.InputField sliceGap;

        public castMesh canvas;

        void OnEnable()
        {
            refreshTexts();
        }

        void refreshTexts()
        {
            if (offsetX)
                offsetX.text = canvas.sliceOffsetX.ToString();
            if (offsetY)
                offsetY.text = canvas.sliceOffsetY.ToString();
            if (sliceW)
                sliceW.text = canvas.sliceWidth.ToString();
            if (sliceH)
                sliceH.text = canvas.sliceHeight.ToString();
            if (sliceGap)
                sliceGap.text = canvas.sliceGap.ToString();
        }

        public void textChanged()
        {
            if (offsetX)
                 canvas.sliceOffsetX = dataFileDict.stringToFloat(offsetX.text, 0f);
            if (offsetY)
                canvas.sliceOffsetY = dataFileDict.stringToFloat(offsetY.text, 0f);
            if (sliceW)
                canvas.sliceWidth = dataFileDict.stringToFloat(sliceW.text, 1600f);
            if (sliceH)
                canvas.sliceHeight = dataFileDict.stringToFloat(sliceH.text, 150f);
            if (sliceGap)
                canvas.sliceGap = dataFileDict.stringToFloat(sliceGap.text, 0f);

            canvas.updateMesh();
        }

        void Update() 
        {
            bool didSomething = false;

            if (UnityEngine.Input.GetKeyDown(nudgeUp))
            {
                canvas.sliceOffsetY += sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(nudgeDown))
            {
                canvas.sliceOffsetY -= sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(nudgeLeft))
            {
                canvas.sliceOffsetX -= sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(nudgeRight))
            {
                canvas.sliceOffsetX += sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(sliceHeightUp))
            {
                canvas.sliceHeight += sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(sliceHeightDown))
            {
                canvas.sliceHeight -= sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(sliceWidthUp))
            {
                canvas.sliceWidth += sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(sliceWidthDown))
            {
                canvas.sliceWidth -= sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(sliceGapUp))
            {
                canvas.sliceGap += sensitivity;
                canvas.sliceHeight -= sensitivity;
                didSomething = true;
            }
            else if (UnityEngine.Input.GetKeyDown(sliceGapDown))
            {
                canvas.sliceGap -= sensitivity;
                canvas.sliceHeight += sensitivity;
                didSomething = true;
            }

            if (didSomething)
            {
                canvas.updateMesh();
                refreshTexts();
            }
	
	    }

    }
}
