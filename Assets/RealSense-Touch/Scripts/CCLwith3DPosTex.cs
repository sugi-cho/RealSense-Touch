using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCLwith3DPosTex : MonoBehaviour
{
    public ComputeShader cclCompute;

    public int width = 512;
    public int height = 512;
    public int numMaxLabels = 32;
    public int numPerLabel = 128;

    public Material souceToInput;
    public Material blobMat;

    public Camera blobDrawer;
    MaterialPropertyBlock mpb;

    [SerializeField] RenderTexture posTex;
    [SerializeField] RenderTexture inputTex;
    [SerializeField] RenderTexture labelTex;

    ComputeBuffer labelFlgBuffer;
    ComputeBuffer labelAppendBuffer;
    ComputeBuffer labelArgBuffer;
    ComputeBuffer posDataAppendBuffer;
    ComputeBuffer posDataBuffer;
    ComputeBuffer accumePosDataBuffer;

    [SerializeField] uint[] args;
    [SerializeField] PosData[] posData;
    [SerializeField] PosDataEvent onTouchEvent;

    Mesh quad
    {
        get
        {
            if (_q == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _q = go.GetComponent<MeshFilter>().sharedMesh;
                Destroy(go);
            }
            return _q;
        }
    }
    Mesh _q;

    [System.Serializable]
    public struct PosData
    {
        public float size;
        public Vector3 pos;
    }

    private void Start()
    {
        posTex = new RenderTexture(width, height, 16, RenderTextureFormat.ARGBFloat);
        posTex.Create();
        GetComponentInParent<Camera>().targetTexture = posTex;

        inputTex = new RenderTexture(width, height, 16, RenderTextureFormat.R8);
        inputTex.Create();
        labelTex = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
        labelTex.filterMode = FilterMode.Point;
        labelTex.enableRandomWrite = true;
        labelTex.Create();


        labelFlgBuffer = new ComputeBuffer(width * height, sizeof(int));
        labelAppendBuffer = new ComputeBuffer(numMaxLabels, sizeof(int), ComputeBufferType.Append);
        labelArgBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        posDataAppendBuffer = new ComputeBuffer(numPerLabel, sizeof(int) * 4, ComputeBufferType.Append);
        posDataBuffer = new ComputeBuffer(numPerLabel * numMaxLabels, sizeof(float) * 4);
        accumePosDataBuffer = new ComputeBuffer(numMaxLabels, sizeof(float) * 4);
        args = new uint[] { quad.GetIndexCount(0), 0, 0, 0, 0 };
        labelArgBuffer.SetData(args);
        posData = new PosData[numMaxLabels];
        mpb = new MaterialPropertyBlock();

        InvokeRepeating("DetectBlobs", 1f / 30f, 1f / 30f);
    }

    private void OnDestroy()
    {
        new List<RenderTexture>(new[] { inputTex, labelTex })
            .ForEach(rt => rt.Release());
        new List<ComputeBuffer>(new[] { labelFlgBuffer, labelAppendBuffer, labelArgBuffer, posDataAppendBuffer, posDataBuffer, accumePosDataBuffer })
            .ForEach(bf => bf.Dispose());
    }

    public void DetectBlobs()
    {
        var kernel = cclCompute.FindKernel("init");
        cclCompute.SetTexture(kernel, "inTex", inputTex);
        cclCompute.SetTexture(kernel, "labelTex", labelTex);
        cclCompute.SetInt("numMaxLabel", numMaxLabels);
        cclCompute.SetInt("texWidth", width);
        cclCompute.SetInt("texHeight", height);
        cclCompute.Dispatch(kernel, width / 8, height / 8, 1);

        kernel = cclCompute.FindKernel("columnWiseLabel");
        cclCompute.SetTexture(kernel, "labelTex", labelTex);
        cclCompute.Dispatch(kernel, width / 8, 1, 1);

        var itr = Mathf.Log(width, 2);
        var div = 2;
        for (var i = 0; i < itr; i++)
        {
            kernel = cclCompute.FindKernel("mergeLabels");
            cclCompute.SetTexture(kernel, "labelTex", labelTex);
            cclCompute.SetInt("div", div);

            cclCompute.Dispatch(kernel, Mathf.Max(width / (2 << i) / 8, 1), 1, 1);
            div *= 2;
        }

        kernel = cclCompute.FindKernel("clearLabelFlag");
        cclCompute.SetTexture(kernel, "labelTex", labelTex);
        cclCompute.SetBuffer(kernel, "labelBuffer", labelAppendBuffer);
        cclCompute.SetBuffer(kernel, "labelFlg", labelFlgBuffer);
        cclCompute.Dispatch(kernel, width * height / 8, 1, 1);

        kernel = cclCompute.FindKernel("setRootLabel");
        cclCompute.SetTexture(kernel, "labelTex", labelTex);
        cclCompute.SetBuffer(kernel, "labelFlg", labelFlgBuffer);
        cclCompute.Dispatch(kernel, width / 8, height / 8, 1);

        labelAppendBuffer.SetCounterValue(0);
        kernel = cclCompute.FindKernel("countLabel");
        cclCompute.SetBuffer(kernel, "labelFlg", labelFlgBuffer);
        cclCompute.SetBuffer(kernel, "labelAppend", labelAppendBuffer);
        cclCompute.Dispatch(kernel, width * height / 8, 1, 1);

        kernel = cclCompute.FindKernel("clearPosData");
        cclCompute.SetBuffer(kernel, "posDataBuffer", posDataBuffer);
        cclCompute.Dispatch(kernel, numPerLabel * numMaxLabels / 8, 1, 1);

        for (var i = 0; i < numMaxLabels; i++)
        {
            cclCompute.SetInt("labelIdx", i);
            cclCompute.SetInt("numPerLabel", numPerLabel);

            posDataAppendBuffer.SetCounterValue(0);
            kernel = cclCompute.FindKernel("clearPosData");
            cclCompute.SetBuffer(kernel, "posDataBuffer", posDataAppendBuffer);
            cclCompute.Dispatch(kernel, numPerLabel / 8, 1, 1);

            kernel = cclCompute.FindKernel("appendPosData");
            cclCompute.SetTexture(kernel, "posTex", posTex);
            cclCompute.SetBuffer(kernel, "labelBuffer", labelAppendBuffer);
            cclCompute.SetTexture(kernel, "labelTex", labelTex);
            cclCompute.SetBuffer(kernel, "posDataAppend", posDataAppendBuffer);
            cclCompute.Dispatch(kernel, width / 8, height / 8, 1);

            kernel = cclCompute.FindKernel("setPosData");
            cclCompute.SetBuffer(kernel, "inPosDataBuffer", posDataAppendBuffer);
            cclCompute.SetBuffer(kernel, "posDataBuffer", posDataBuffer);
            cclCompute.Dispatch(kernel, numPerLabel / 8, 1, 1);
        }

        kernel = cclCompute.FindKernel("buildPosData");
        cclCompute.SetBuffer(kernel, "inPosDataBuffer", posDataBuffer);
        cclCompute.SetBuffer(kernel, "posDataBuffer", accumePosDataBuffer);
        cclCompute.Dispatch(kernel, 1, numMaxLabels, 1);

        ComputeBuffer.CopyCount(labelAppendBuffer, labelArgBuffer, sizeof(uint));
        labelArgBuffer.GetData(args);
        accumePosDataBuffer.GetData(posData);

        onTouchEvent.Invoke(posData);
    }

    Vector4 prop;
    private void Update()
    {
        if (blobDrawer == null)
            return;
        prop.x = 1f / width;
        prop.y = 1f / height;
        prop.z = blobDrawer.orthographicSize;
        prop.w = blobDrawer.aspect * prop.z;
        mpb.SetVector("_Prop", prop);
        mpb.SetBuffer("_LabelBuffer", accumePosDataBuffer);
        Graphics.DrawMeshInstancedIndirect(
            quad, 0, blobMat, quad.bounds, labelArgBuffer, 0, mpb,
            UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, blobDrawer);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, inputTex, souceToInput);
        Graphics.Blit(source, posTex);
        Graphics.Blit(source, destination);
    }

    private void OnDrawGizmosSelected()
    {
        for (var i = 0; i < posData.Length; i++)
            if (0 < posData[i].size)
                Gizmos.DrawSphere(posData[i].pos, 0.1f);
    }

    [System.Serializable]
    public class PosDataEvent : UnityEngine.Events.UnityEvent<PosData[]> { }
}