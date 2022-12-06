using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public static class Extent 
{
    // Start is called before the first frame update
    public static bool IsPointInExtent((Vector3 min, Vector3 max) extent, Vector3 point) =>
        point.x >= extent.min.x && point.y >= extent.min.y && point.z >= extent.min.z &&
        point.x <= extent.max.x && point.y <= extent.max.y && point.z <= extent.max.z;
    public static bool CheckIntersection((Vector3 min, Vector3 max) extentA, (Vector3 min, Vector3 max) extentB) => CheckIntersection(extentA, extentB, false);
    private static bool CheckIntersection((Vector3 min, Vector3 max) extentA, (Vector3 min, Vector3 max) extentB, bool isSecond) =>
        IsPointInExtent(extentA, new Vector3(extentB.min.x, extentB.min.y, extentB.min.z)) ||
        IsPointInExtent(extentA, new Vector3(extentB.min.x, extentB.min.y, extentB.max.z)) ||
        IsPointInExtent(extentA, new Vector3(extentB.min.x, extentB.max.y, extentB.min.z)) ||
        IsPointInExtent(extentA, new Vector3(extentB.min.x, extentB.max.y, extentB.max.z)) ||
        IsPointInExtent(extentA, new Vector3(extentB.max.x, extentB.min.y, extentB.min.z)) ||
        IsPointInExtent(extentA, new Vector3(extentB.max.x, extentB.min.y, extentB.max.z)) ||
        IsPointInExtent(extentA, new Vector3(extentB.max.x, extentB.max.y, extentB.min.z)) ||
        IsPointInExtent(extentA, new Vector3(extentB.max.x, extentB.max.y, extentB.max.z)) ? true :
        isSecond ? false : CheckIntersection(extentB, extentA, true);
        
    public static ((int x, int y, int z) min, (int x, int y, int z) max) UpdateExtent(IEnumerable<Vector3> verts)
    {
        var min = verts.First();
        var max = verts.First();
        var ids = (min: (x: 0, y: 0, z: 0), max: (x: 0, y: 0, z: 0));
        int i = 0;
        foreach(var v in verts)
        {
            if (v.x < min.x) { ids.min.x = i; min.x = v.x; }
            else if (v.x > max.x) { ids.max.x = i; max.x = v.x; }

            if (v.y < min.y) { ids.min.y = i; min.y = v.y; }
            else if (v.y > max.y) { ids.max.y = i; max.y = v.y; }

            if (v.z < min.z) { ids.min.z = i; min.z = v.z; }
            else if (v.z > max.z) { ids.max.z = i; max.z = v.z; }

            i++;
        }
        return ids;
    }
}
