using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebCamCCL : MonoBehaviour
{

    public int width = 640;
    public int height = 480;
    public Material visualizer;
    public CCL ccl;
    WebCamTexture webcamTex;

    [Header("for tuning")]
    public bool countLabel;
    public bool buildBlob;

    // Use this for initialization
    void Start()
    {
        webcamTex = new WebCamTexture(width, height);
        webcamTex.Play();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(webcamTex, destination);
    }
}
