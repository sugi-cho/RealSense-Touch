using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawableCanvas : MonoBehaviour {

    public Material drawCanvas;
    RenderTexture[] rts;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (rts == null || source.width != rts[0].width || source.height != rts[1].height)
            CreateCanvas(source);
        Graphics.Blit(rts[1], destination);
    }

    void CreateCanvas(RenderTexture source)
    {
        if (rts != null)
            for (var i = 0; i < 2; i++)
                rts[i].Release();
        rts = new RenderTexture[2];
        for(var i = 0; i < 2; i++)
        {
            var rt = new RenderTexture(source.width, source.height, 16, RenderTextureFormat.ARGBHalf);
            rt.Create();
            rts[i] = rt;
        }
        ClearCanvas();
    }
    void ClearCanvas()
    {
        var tmp = RenderTexture.active;
        RenderTexture.active = rts[0];
        GL.Clear(true, true, Color.black, 0);
        RenderTexture.active = tmp;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ClearCanvas();
        Graphics.Blit(rts[0], rts[1], drawCanvas);
        SwapArray(rts);
        Graphics.CopyTexture(rts[0], rts[1]);
    }

    void SwapArray<T>(T[] array)
    {
        var tmp = array[0];
        array[0] = array[1];
        array[1] = tmp;
    }
}
