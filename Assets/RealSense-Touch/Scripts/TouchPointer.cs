using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchPointer : MonoBehaviour
{
    public bool isTracking;
    public bool trackFlg;

    public Vector3 worldPos;
    public Vector3 screenPos;
    public bool touching;
    public float lastUpdated;

    TouchPointManager manager;

    private void OnDrawGizmos()
    {
        if (isTracking)
        {
            var col = touching ? Color.red : Color.green;
            col.a = 0.3f;
            Gizmos.color = col;
            Gizmos.DrawSphere(worldPos, 0.1f);
        }
    }
}
