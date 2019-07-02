using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseDebugger : MonoBehaviour
{
    public TouchPointManager touchManager;
    public Transform[] debugObjs;
    Transform debugObj { get { return debugObjs[0]; } }
    Camera touchAreaCam;

    private void Start()
    {
        touchAreaCam = touchManager.GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        var pos = Input.mousePosition;

        pos.x /= Screen.width;
        pos.y /= Screen.height;
        pos.z = Input.GetMouseButton(0) ? touchAreaCam.farClipPlane : touchAreaCam.nearClipPlane;
        pos = touchAreaCam.ViewportToWorldPoint(pos);
        debugObj.position = pos;

        var ts = Input.touches;
        if (0 < ts.Length)
        {
            for (var i = 0; i < ts.Length; i++)
            {
                var t = ts[i];
                pos = t.position;
                pos.x /= Screen.width;
                pos.y /= Screen.height;
                pos.z = t.phase == TouchPhase.Ended ? touchAreaCam.nearClipPlane : touchAreaCam.farClipPlane;
                pos = touchAreaCam.ViewportToWorldPoint(pos);
                debugObjs[i % debugObjs.Length].position = pos;
            }
        }
    }
}
