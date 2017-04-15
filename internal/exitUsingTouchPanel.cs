using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//adding this component to any gameObject will create a button on the upper left of Volume that will cause the application to quit if you hold it for 
//exitHoldTime seconds.  If both the top left and top right are held, it will cause the application to print out a debug report

//alternatively, pressing Left CTRL + i  will also print out a debug report

namespace hypercube
{
    public class exitUsingTouchPanel : hypercube.touchScreenTarget
    {

        public sliceModifier overlay;
        public float exitHoldTime = 3f;
        public float activeCornerArea = .1f;

        public bool allowDebugReport = true;

        float timer = 0;
        int activeTouch = -1;

        private void Start()
        {
            stop();
        }

        public override void onTouchDown(hypercube.touch touch)
        {
            if (touch.posX < activeCornerArea && touch.posY < activeCornerArea)  //upper left
            {
                activeTouch = (int)touch.id;
                start();
            }
        }

        public override void onTouchMoved(hypercube.touch touch)
        {
            if ((int)touch.id != activeTouch)
                return;

            if (touch.posX > activeCornerArea || touch.posY > activeCornerArea) //upper left
            {
                stop();
            }
        }

        public override void onTouchUp(hypercube.touch touch)
        {
            if ((int)touch.id == activeTouch)
            {
                stop();
            }
        }


        void Update()
        {

            if (allowDebugReport && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.I)) //print the debug report via hotkey
            {
                utils.writeDebugStats();
                stop();
                return;
            }

            if (activeTouch == -1)
                return;

            timer += Time.deltaTime;
            if (timer > exitHoldTime)
            {
                //check for the finger on the upper right
                if (allowDebugReport)
                { 
                    foreach (touch i in hypercube.input.touchPanel.front.touches)
                    {
                        if (i.posX > 1f - activeCornerArea || i.posY > activeCornerArea) //upper right
                        {
                            utils.writeDebugStats();
                            stop();
                            return;
                        }
                    }
                }

                //no finger on the upper right, so quit.
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        void start()
        {
            if (!overlay.tex || !hypercubeCamera.mainCam || !castMesh.canvas)
            {
                stop();
                return;
            }

            hypercubeCamera.mainCam.sliceModifiers.Add(overlay);
            sliceModifier.updateSliceModifiers();
        }

        void stop()
        {
            activeTouch = -1;
            timer = 0;

            if (hypercubeCamera.mainCam)
            { 
                hypercubeCamera.mainCam.sliceModifiers.Remove(overlay);
                sliceModifier.updateSliceModifiers();
            }
        }
    }
}
