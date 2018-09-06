using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Intel.RealSense;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;
using System.Threading;

//this class is copy of RealSensePointCloudGenerator class
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RealMesh : MonoBehaviour
{
    public Stream stream = Stream.Depth;
    Mesh mesh;

    PointCloud pc;

    private GCHandle handle;

    Vector3[] vertices;
    IntPtr verticesPtr;
    ComputeBuffer vertBuffer;

    readonly AutoResetEvent e = new AutoResetEvent(false);

    void Start()
    {
        RealSenseDevice.Instance.OnStart += OnStartStreaming;
        RealSenseDevice.Instance.OnStop += OnStopStreaming;
    }

    private void OnStartStreaming(PipelineProfile activeProfile)
    {
        pc = new PointCloud();

        using (var profile = activeProfile.GetStream(stream))
        {
            if (profile == null)
            {
                Debug.LogWarningFormat("Stream {0} not in active profile", stream);
            }
        }

        using (var profile = activeProfile.GetStream(Stream.Depth) as VideoStreamProfile)
        {
            Assert.IsTrue(SystemInfo.SupportsTextureFormat(TextureFormat.RGFloat));

            vertices = new Vector3[profile.Width * profile.Height];
            handle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            verticesPtr = handle.AddrOfPinnedObject();

            vertBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);

            var indices = Enumerable.Range(0, vertices.Length).ToArray();

            mesh = new Mesh()
            {
                indexFormat = IndexFormat.UInt32,
            };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Points, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

            GetComponent<MeshFilter>().sharedMesh = mesh;
            var r = GetComponent<Renderer>();
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetBuffer("_VertBuffer", vertBuffer);
            r.SetPropertyBlock(mpb);
        }

        RealSenseDevice.Instance.onNewSampleSet += OnFrames;
    }

    void OnDestroy()
    {
        OnStopStreaming();
    }

    private void OnStopStreaming()
    {
        e.Reset();

        if (vertBuffer != null)
            vertBuffer.Dispose();
        vertBuffer = null;

        if (handle.IsAllocated)
            handle.Free();

        if (pc != null)
        {
            pc.Dispose();
            pc = null;
        }
    }

    private void OnFrames(FrameSet frames)
    {
        using (var depthFrame = frames.DepthFrame)
        using (var points = pc.Calculate(depthFrame))
        using (var f = frames.FirstOrDefault<VideoFrame>(stream))
        {
            pc.MapTexture(f);
            memcpy(verticesPtr, points.VertexData, points.Count * 3 * sizeof(float));

            e.Set();
        }
    }

    void Update()
    {
        if (e.WaitOne(0))
            vertBuffer.SetData(vertices);
    }

    [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    internal static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);
}
