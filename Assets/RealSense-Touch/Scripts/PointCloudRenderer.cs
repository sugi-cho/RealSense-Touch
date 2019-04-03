using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

using Intel.RealSense;

public class PointCloudRenderer : MonoBehaviour
{
    public RsFrameProvider pointSource;

    public Texture Texture
    {
        set
        {
            var kernel = pointCloudBuilder.FindKernel("buildPointCloud");
            texture = value;
            pointCloudBuilder.SetTexture(kernel, "_InfraredTex", texture);
        }
    }
    [SerializeField] Texture texture;
    [SerializeField] Texture2D mapTex;
    [SerializeField] Texture2D posTex;

    FrameQueue pointQueue;

    [Header("compute pointCloud")]
    public ComputeShader pointCloudBuilder;
    ComputeBuffer pointCloudBuffer;

    Mesh mesh;

    private void Start()
    {
        pointQueue = new FrameQueue();

        pointSource.OnNewSample += PointSource_OnNewSample;
    }

    private void PointSource_OnNewSample(Frame frame)
    {
        using (var pf = RetrievePointFrame(frame))
            if (pf != null) pointQueue.Enqueue(pf);
    }

    private void Update()
    {
        using (var pf = DequeuePointFrame())
            if (pf != null)
            {
                if (mapTex == null || mapTex.width * mapTex.height != pf.Count)
                    using (var p = pf.Profile as VideoStreamProfile)
                        CreateResources(p.Width, p.Height);

                if (pf.TextureData != IntPtr.Zero)
                {
                    mapTex.LoadRawTextureData(pf.TextureData, pf.Count * 2 * sizeof(float));
                    mapTex.Apply();
                }
                if (pf.VertexData != IntPtr.Zero)
                {
                    posTex.LoadRawTextureData(pf.VertexData, pf.Count * 3 * sizeof(float));
                    posTex.Apply();
                }
            }

        if (pointCloudBuffer != null && texture != null && mapTex != null && posTex != null)
        {
            var kernel = pointCloudBuilder.FindKernel("buildPointCloud");
            pointCloudBuilder.SetTexture(kernel, "_Tex", texture);
            pointCloudBuilder.Dispatch(kernel, texture.width / 8, texture.height / 8, 1);
        }
    }

    Points RetrievePointFrame(Frame frame)
    {
        if (frame is Points) return (Points)frame;

        if (frame.IsComposite)
        {
            using (var fset = FrameSet.FromFrame(frame))
            {
                foreach (var f in fset)
                {
                    var ret = RetrievePointFrame(f);
                    if (ret != null) return ret;
                    f.Dispose();
                }
            }
        }

        return null;
    }
    Points DequeuePointFrame()
    {
        Frame frame;
        return pointQueue.PollForFrame(out frame) ? (Points)frame : null;
    }

    void CreateResources(int width, int height)
    {
        if (mapTex != null)
            Destroy(mapTex);
        if (posTex != null)
            Destroy(posTex);

        mapTex = new Texture2D(width, height, TextureFormat.RGFloat, false);
        posTex = new Texture2D(width * 3, height, TextureFormat.RFloat, false);

        if (pointCloudBuffer != null)
            pointCloudBuffer.Dispose();
        pointCloudBuffer = new ComputeBuffer(width * height, sizeof(float) * 4);

        Application.quitting += () =>
        {
            if (pointCloudBuffer != null)
                pointCloudBuffer.Dispose();
        };

        var kernel = pointCloudBuilder.FindKernel("buildPointCloud");
        pointCloudBuilder.SetTexture(kernel, "_RemapTex", mapTex);
        pointCloudBuilder.SetTexture(kernel, "_PosTex", posTex);
        pointCloudBuilder.SetBuffer(kernel, "_Output", pointCloudBuffer);
        pointCloudBuilder.SetInt("_Width", width);
        pointCloudBuilder.SetInt("_Height", height);

        if (mesh == null)
            mesh = new Mesh()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            };
        var vs = Enumerable.Repeat(Vector3.zero, width * height).ToList();
        var indeces = vs.Select((v, i) => i).ToArray();
        mesh.SetVertices(vs);
        mesh.SetIndices(indeces, MeshTopology.Points, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100f);


        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshRenderer>().material.SetBuffer("_PCBuffer", pointCloudBuffer);
    }
}
