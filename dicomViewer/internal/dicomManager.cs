using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class dicomManager : MonoBehaviour {

    public Material baseMat;
    public string defaultPath;

    public UnityEngine.UI.InputField path;

    public UnityEngine.UI.Slider speedSlider;
    public UnityEngine.UI.Text frameText;
    public UnityEngine.UI.Slider focusSlider;
    public UnityEngine.UI.Text focusNumber;
    public UnityEngine.UI.Slider clampSlider;
    public UnityEngine.UI.Text clampNumber;
    public UnityEngine.UI.Slider brightnessSlider;
    public UnityEngine.UI.Dropdown colorScheme;
    public UnityEngine.UI.Slider thicknessSlider;

    public dicomMeshLoader[] frames;

    public UnityEngine.UI.Dropdown shaderDropDown;
    public Shader[] shaderOptions;

    int currentFrame = 0;
    bool playing = false;
    public float playSpeed = .3f;
    float playTimer = 0f;

    void Start()
    {
        load();

       shaderDropDown.ClearOptions();
       List<string> names = new List<string>();
        foreach(Shader s in shaderOptions)
        {
            names.Add(System.IO.Path.GetFileNameWithoutExtension(s.name)); //save the base name
        }
        shaderDropDown.AddOptions(names);
        
    }

    public void updateSettings()
    {
        defaultPath = path.text;
        playSpeed = speedSlider.value;
        focusNumber.text = focusSlider.value.ToString();
        clampNumber.text = clampSlider.value.ToString();

        foreach (dicomMeshLoader g in frames)
        {
            MeshRenderer r = g.getRenderer();
            if (r)
            {
                r.material.SetFloat("_Focus", focusSlider.value);             
                r.material.SetFloat("_Clamp", clampSlider.value);             
                r.material.SetFloat("_Mod", brightnessSlider.value);

                float uv = .95f - ((float)colorScheme.value / (float)colorScheme.options.Count); //.95 instead of 1f puts it  into the middle of the texture so it doesn't bleed to the next line
                r.material.SetFloat("_Lookup", uv);
            }
        }
    }

    public void updateThickness()
    {
        foreach (dicomMeshLoader d in frames)
        {
            if (d)
            {
                d.updateThickness(thicknessSlider.value);
            }
        }
    }

    public void load()
    {
        if (defaultPath != "")          
            load(defaultPath);
        updateSettings();
    }
    public virtual void load(string dirPath)
    {
        //clean up first.
        if (frames != null && frames.Length > 0)
        {
            foreach (dicomMeshLoader m in frames)
            {
                Destroy(m.getRenderer().material.mainTexture); //these textures can be very big. so be sure to clean them up.
                Destroy(m.gameObject);
            }
        }

        string[] subdirs = Directory.GetDirectories(dirPath);

        if (subdirs.Length == 0) // no sub directores? try loading the given dir as a frame.
        {
            frames = new dicomMeshLoader[1]; //1 frame only
            makeFrame(0, dirPath, baseMat);
            setFrame(0);
            return;
        }

        frames = new dicomMeshLoader[subdirs.Length];
        for(int d = 0; d <subdirs.Length; d++ )
        {
            makeFrame(d, subdirs[d], baseMat);
        }
        setFrame(0);
    }

    void makeFrame(int frameNum, string path, Material baseMat)
    {
        GameObject g = new GameObject("frame_" + frameNum);

        //parent to ourselves and zero it out.
        g.transform.parent = transform;
        g.transform.localPosition = Vector3.zero;
        g.transform.localRotation = Quaternion.identity;

        frames[frameNum] = g.AddComponent<dicomMeshLoader>();
        frames[frameNum].loadFrame(path, baseMat);

        g.SetActive(false);
    }

    public void nextFrame()
    {
        currentFrame++;
        if (currentFrame >= frames.Length)
            currentFrame = 0;
        setFrame(currentFrame);
    }

    public void prevFrame()
    {
        currentFrame--;
        if (currentFrame < 0)
            currentFrame = frames.Length - 1;
        setFrame(currentFrame);
    }

    public void setFrame(int f)
    {
        for(int i = 0; i < frames.Length; i ++)
        {
            if (f == i && !frames[i].gameObject.activeSelf)
                frames[i].gameObject.SetActive(true);
            else if (f != i && frames[i].gameObject.activeSelf)
                frames[i].gameObject.SetActive(false);
        }

        frameText.text = f + "/" + frames.Length;
    }

    public void play()
    {
        playing = true;
    }
    public void pause()
    {
        playing = false;
    }

    public void onSetShader()
    {
        foreach (dicomMeshLoader m in frames)
        {
            m.gameObject.GetComponent<MeshRenderer>().sharedMaterial.shader = shaderOptions[shaderDropDown.value];
        }
    }

    void Update()
    {
        if (!playing)
            return;

        playTimer -= Time.deltaTime;
        if (playTimer < 0)
        {
            nextFrame();
            playTimer = playSpeed;
        }
    }

}
