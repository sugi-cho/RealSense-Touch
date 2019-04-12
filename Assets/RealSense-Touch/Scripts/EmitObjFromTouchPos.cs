using System.Linq;
using UnityEngine;

public class EmitObjFromTouchPos : MonoBehaviour
{
    public GameObject obj;

    public void OnTouchData(CCLwith3DPos.PosData[] posData)
    {
        for (var i = 0; i < posData.Length; i++)
            if (0 < posData[i].size)
            {
                var newObj = Instantiate(obj);
                newObj.transform.position = posData[i].pos;
                Destroy(newObj, 10f);
            }
    }
}
