using UnityEngine;
using System.Collections;


[ExecuteInEditMode]
public class focusStackerTester : MonoBehaviour {

    public focusStackedEffect fs;
    public RenderTexture source;
    public RenderTexture target;

	// Use this for initialization
	void Start () {
	
	}
		
	
	// Update is called once per frame
	void Update () {

        fs.processFrame(source, target);
	}
}
