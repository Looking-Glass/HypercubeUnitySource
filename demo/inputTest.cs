using UnityEngine;
using System.Collections;

public class inputTest : MonoBehaviour 
{
    public Transform[] fingerItems;

    public hypercubeCamera cam;

    public Transform someThing;

	void Update () 
    {
        if (hypercube.input.touchPanel == null) //Volume not connected via USB
            return;  

        for (uint i = 0; i < fingerItems.Length; i++)
        {
            if (i < hypercube.input.touchPanel.touches.Length)
                fingerItems[i].position = hypercube.input.touchPanel.touches[i].getWorldPos(cam);
            else
                fingerItems[i].position = new Vector3(50f, 50f, 50f); //not used atm, just put them aside.
        }

        someThing.transform.Translate(hypercube.input.touchPanel.averageDiff.x, hypercube.input.touchPanel.averageDiff.y, 0f, Space.World);

        someThing.Rotate(0f, hypercube.input.touchPanel.front.twist, 0f); 

        someThing.localScale *= hypercube.input.touchPanel.pinch;
        
	}
}
