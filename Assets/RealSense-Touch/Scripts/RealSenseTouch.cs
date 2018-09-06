using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealSenseTouch : MonoBehaviour
{
    public float screenWidth = 2f;
    public float screenHeight = 1.5f;
    public int texSize = 512;

    public Material targetMat;
    public string texName;

    int height { get { return texSize; } }
    int width { get { return Mathf.FloorToInt(texSize / screenHeight * screenWidth); } }

    RenderTexture targetTex;
    Camera cam { get { if (_c == null) _c = GetComponentInChildren<Camera>(); return _c; } }
    Camera _c;

    CCL cclProcessor;

    // Use this for initialization
    void Start()
    {
        cclProcessor = GetComponentInChildren<CCL>();
        CreateTex();
    }

    private void OnDestroy()
    {
        if (targetTex != null)
            targetTex.Release();
    }

    void CreateTex()
    {
        if (targetTex != null)
            targetTex.Release();
        targetTex = new RenderTexture(width, height, 16, RenderTextureFormat.RFloat);
        targetTex.wrapMode = TextureWrapMode.Clamp;
        targetTex.Create();
        cam.targetTexture = targetTex;
        cam.orthographicSize = screenHeight * 0.5f;
    }

    private void Update()
    {
        if (targetTex == null || targetTex.width != width || targetTex.height != height)
            CreateTex();
        cclProcessor.Compute(targetTex);
        targetMat.SetTexture(texName, cclProcessor.output);
    }
}
