using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

using Intel.RealSense;

public class RealDepthMesh : MonoBehaviour
{
    public RsFrameProvider pointSource;

    FrameQueue pointQueue;

    Vector3[] vertices;
    GCHandle handle;
    IntPtr verticesPtr;
    Mesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        pointSource.OnStart += OnStart;
        pointSource.OnStop += OnStop;
    }
    private void OnDestroy()
    {
        if (pointQueue != null)
            pointQueue.Dispose();
        if (handle.IsAllocated)
            handle.Free();
    }

    void OnStart(PipelineProfile obj)
    {
        pointQueue = new FrameQueue(1);
        using (var depth = obj.Streams.FirstOrDefault(s => s.Stream == Stream.Depth) as VideoStreamProfile)
            CreateResources(depth.Width, depth.Height);
        pointSource.OnNewSample += OnNewSample;
    }
    void OnNewSample(Frame frame)
    {
        using (var pf = RetrievePointFrame(frame))
            if (pf != null) pointQueue.Enqueue(pf);
    }
    void OnStop()
    {
        pointSource.OnNewSample -= OnNewSample;
        if (pointQueue != null)
            pointQueue.Dispose();
        if (handle.IsAllocated)
            handle.Free();
    }

    // Update is called once per frame
    void Update()
    {
        if (pointQueue != null)
            using (var pf = DequeuePointFrame())
                if (pf != null)
                {
                    if (pf.Count != mesh.vertexCount)
                    {
                        using (var p = pf.Profile as VideoStreamProfile)
                            CreateResources(p.Width, p.Height);
                    }
                    if (pf.VertexData != IntPtr.Zero)
                    {
                        memcpy(verticesPtr, pf.VertexData, pf.Count * sizeof(float) * 3);
                        mesh.vertices = vertices;
                        mesh.UploadMeshData(false);
                    }
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
        vertices = new Vector3[width * height];
        if (handle.IsAllocated)
            handle.Free();
        handle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        verticesPtr = handle.AddrOfPinnedObject();

        var indices = new int[(width - 1) * (height - 1) * 6];

        for (var y = 0; y < height - 1; y++)
            for (var x = 0; x < width - 1; x++)
            {
                var idx = (y * (width - 1) + x) * 6;
                var i0 = y * width + x;
                var i1 = i0 + 1;
                var i2 = i0 + width;
                var i3 = i1 + width;

                indices[idx + 0] = i0;
                indices[idx + 1] = i2;
                indices[idx + 2] = i3;

                indices[idx + 3] = i0;
                indices[idx + 4] = i3;
                indices[idx + 5] = i1;
            }

        if (mesh != null)
            mesh.Clear();
        else
            mesh = new Mesh()
            {
                indexFormat = IndexFormat.UInt32,
            };
        mesh.MarkDynamic();
        mesh.vertices = vertices;

        var uvs = new Vector2[width * height];
        Array.Clear(uvs, 0, uvs.Length);
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                uvs[i + j * width].x = i / (float)width;
                uvs[i + j * width].y = j / (float)height;
            }
        }

        mesh.uv = uvs;

        mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

        GetComponent<MeshFilter>().sharedMesh = mesh;

        var mpb = new MaterialPropertyBlock();
        GetComponent<Renderer>().SetPropertyBlock(mpb);
    }

    [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    internal static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);
}
