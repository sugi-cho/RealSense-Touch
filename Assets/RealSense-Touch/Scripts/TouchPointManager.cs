using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class TouchPointManager : MonoBehaviour, CCLwith3DPos.I3DTouchAction
{
    public int nMaxPoints = 10;
    [Header("size limit")]
    public float minSize = 3;
    public float maxSize = 30;
    [Header("merge and track")]
    public float mergeRange = 0.05f;
    public float trackRange = 0.1f;
    public float touchThreshold = 0.5f;
    public float lostDuration = 0.2f;
    [Header("touch events")]
    public TouchPointerEvent onFindPointer;
    public TouchPointerEvent onTouchDown;
    public TouchPointerEvent onTouch;
    public TouchPointerEvent onTouchUp;
    public TouchPointerEvent onLostPointer;

    TouchPointer[] points;
    List<CCLwith3DPos.PosData> rawDataList;

    public void OnTouch(Camera cam, CCLwith3DPos.PosData[] posData)
    {
        rawDataList.Clear();

        foreach (var pd in posData)
            if (minSize <= pd.size && pd.size <= maxSize)
                rawDataList.Add(pd);

        //近くの点をマージする
        for (var i = 0; i < rawDataList.Count; i++)
            if (0 < rawDataList[i].size)
            {
                var current = rawDataList[i];
                var nears = rawDataList
                    .Select((data, idx) => new { data, idx })
                    .Where(pair => i < pair.idx && 0 < pair.data.size)
                    .Where(pair => (pair.data.pos - current.pos).sqrMagnitude < mergeRange * mergeRange)
                    .ToList();

                var accumPos = current.pos;
                for (var n = 0; n < nears.Count; n++)
                {
                    var near = nears[n];
                    var data = near.data;
                    var idx = near.idx;
                    accumPos += data.pos;
                    //マージされたpointのsizeは0にする
                    data.size = 0;
                    rawDataList[idx] = data;
                }
                current.pos = accumPos / (nears.Count + 1);
                rawDataList[i] = current;
                nears.Clear();
                nears = null;
            }
        foreach (var p in points)
            p.trackFlg = false;

        //既存の点のトラッキング処理、新規タッチ処理
        for (var i = 0; i < rawDataList.Count; i++)
            if (0 < rawDataList[i].size)
            {
                var data = rawDataList[i];
                var tracking = points
                    .Where(p => p.isTracking && !p.trackFlg)
                    .Where(p => (p.worldPos - data.pos).sqrMagnitude < trackRange * trackRange)
                    .OrderBy(p => (p.worldPos - data.pos).sqrMagnitude)
                    .FirstOrDefault();

                if (tracking == null)
                {
                    var newPoint = points.Where(p => !p.isTracking).FirstOrDefault();
                    if (newPoint != null)
                    {
                        newPoint.isTracking = true;
                        tracking = newPoint;
                        onFindPointer.Invoke(newPoint);
                    }
                }
                if (tracking != null)
                {
                    tracking.transform.position = tracking.worldPos = data.pos;
                    tracking.screenPos = cam.WorldToViewportPoint(data.pos);
                    tracking.lastUpdated = Time.time;
                    tracking.trackFlg = true;

                    if (touchThreshold < tracking.screenPos.z)
                    {
                        if (!tracking.touching)
                            onTouchDown.Invoke(tracking);
                        tracking.touching = true;
                        onTouch.Invoke(tracking);
                    }
                    else if (tracking.touching)
                    {
                        onTouchUp.Invoke(tracking);
                        tracking.touching = false;
                    }
                }
            }

        //しばらく更新されてないPointの削除
        for (var i = 0; i < points.Length; i++)
            if (lostDuration < Time.time - points[i].lastUpdated)
            {
                points[i].isTracking = false;
                onLostPointer.Invoke(points[i]);
            }
    }


    // Start is called before the first frame update
    void Start()
    {
        rawDataList = new List<CCLwith3DPos.PosData>();
        points = new TouchPointer[nMaxPoints];
        for (var i = 0; i < nMaxPoints; i++)
        {
            points[i] = new GameObject($"point_{i.ToString("00")}").AddComponent<TouchPointer>();
            points[i].isTracking = false;
            points[i].transform.SetParent(transform);
            points[i].transform.localPosition = Vector3.zero;
        }
    }

    [System.Serializable]
    public class TouchPointerEvent : UnityEvent<TouchPointer> { }
}
