using UnityEngine;
using System.Collections;

namespace hypercube
{
    [System.Serializable]
    public class calibrationStage
    {
        public string name;  //not functional, just for inspector convenience
        public Texture infoTexture;
        public GameObject[] activeObjects;
       
    }

    public class calibrationStager : MonoBehaviour
    {
        protected int stage = 0;

        public castMesh canvas;
        public calibrationStage[] stages;

        
       // public GameObject infoScreen;

        float checkUSBtimer = 0f;

        void Start()
        {
            stage = 0;
            resetStage();
        }

        void Update()
        {
            //don't progress without the usb...
            if (stage == 0)
            {
                if (canvas.foundConfigFile)
                    nextStage();
                else
                {
                    checkUSBtimer += Time.deltaTime;
                    if (checkUSBtimer > 2f) //check every 2 seconds
                    {
                        canvas.loadSettings();
                        checkUSBtimer = 0;
                    }
                }
            }

            //normal path...

            if (Input.GetKeyDown(KeyCode.RightBracket))
                nextStage();
            else if (Input.GetKeyDown(KeyCode.LeftBracket))
                prevStage();

            if (Input.GetKeyDown(KeyCode.Escape)) //go to next stage
            {
                quit();
                return;
            }
        }

        public void nextStage()
        {
            stage++;

            if (stage >= stages.Length)
                stage = 0;

            resetStage();
        }
        public void prevStage()
        {
            stage--;

            if (stage < 0)
                stage = 0;  //don't loop

            resetStage();
        }
        void resetStage()
        {
            //disable all.
            foreach (calibrationStage s in stages)
            {
                foreach (GameObject o in s.activeObjects)
                {
                    o.SetActive(false);
                }
            }

            //enable the appropriate things.
            foreach (GameObject o in stages[stage].activeObjects)
            {
                o.SetActive(true);
            }

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

}


