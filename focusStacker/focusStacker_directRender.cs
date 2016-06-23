using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

//this script will load up a series of  images as a focus stack
//it will process it directly onto the hypercube slice images (bypasses the hypercube camera)
//if you supply a directory it will use that on start, otherwise it will try to use what is in sources

[ExecuteInEditMode]
[RequireComponent (typeof(focusStackedEffect))]
public class focusStacker_directRender : MonoBehaviour {

    public string directory;
    public bool updateTextures; //forces a call to OnValidate when changed
    public Texture[] sources;
    public RenderTexture[] outTargets;

    

    focusStackedEffect fs;


	void Start () {
        fs = GetComponent<focusStackedEffect>();


        if (directory != "")
            loadDir(directory);

        updateAllSlices();
	}

    public void updateAllSlices()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            updateSlice(i);
        }
    }

    public void updateSlice(int s)
    {
        if (!fs)
            return;

        if (s < sources.Length  && s < outTargets.Length)
        {
            fs.processFrame(sources[s], outTargets[s]);
        }
    }

    void OnValidate()
    {
        updateAllSlices();
    }

    public void loadDir(string dirPath)
    {
        // find out how may slices it has
        var info = new DirectoryInfo(dirPath);
        FileInfo[] fileInfo = info.GetFiles();
        List<FileInfo> slices = new List<FileInfo>();
        foreach (FileInfo file in fileInfo)
        {
            if (file.Name.EndsWith(".png") ||
                        file.Name.EndsWith(".jpg") ||
                        file.Name.EndsWith(".jpeg") ||
                        file.Name.EndsWith(".tif") ||
                        file.Name.EndsWith(".bmp")
                )
                slices.Add(file);
        }

        if (slices.Count == 0)
        {
            Debug.LogError("No textures found in: " + dirPath);
            return;
        }

        List<Texture2D> textures = new List<Texture2D>();
        foreach (FileInfo s in slices)
        {
            //load the first texture to get the stats
            Texture2D tempTex = new Texture2D(2, 2);
            byte[] pngBytes = File.ReadAllBytes(s.FullName);
            if (!tempTex.LoadImage(pngBytes))        // Load data into the texture.
            {
                Debug.LogError("The texture file: " + s.FullName + " could not be loaded.");
                return;
            }

            textures.Add(tempTex);
        }

        sources = textures.ToArray();
    }

}
