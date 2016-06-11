using UnityEngine;
using System.Collections;


[ExecuteInEditMode]
public class focusStackerTest : MonoBehaviour {

    public focusStacker fs;
    public RenderTexture source;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        fs.processFrame(source);
	}
}
