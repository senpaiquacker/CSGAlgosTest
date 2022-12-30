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
    private static bool CheckIntersection((Vector3 min, Vector3 max) extentA, (Vector3 min, Vector3 max) extentB, bool isSecond)
    {
        bool isAnyInExtent = false;
        foreach(var p in GetAllPoints(extentB))
            isAnyInExtent = isAnyInExtent || IsPointInExtent(extentA, p);
        return isAnyInExtent ? true : isSecond ? false : CheckIntersection(extentB, extentA, true);
    }
    public static Vector3[] GetAllPoints((Vector3 min, Vector3 max) extent, PrimitiveMesh parent = null)
    {
        extent = parent != null ? (parent.ToGlobal(extent.min), parent.ToGlobal(extent.max)) : extent;
        var answ = new Vector3[8];
        answ[0] = new Vector3(extent.min.x, extent.min.y, extent.min.z);
        answ[1] = new Vector3(extent.min.x, extent.min.y, extent.max.z);
        answ[2] = new Vector3(extent.min.x, extent.max.y, extent.min.z);
        answ[3] = new Vector3(extent.min.x, extent.max.y, extent.max.z);
        answ[4] = new Vector3(extent.max.x, extent.min.y, extent.min.z);
        answ[5] = new Vector3(extent.max.x, extent.min.y, extent.max.z);
        answ[6] = new Vector3(extent.max.x, extent.max.y, extent.min.z);
        answ[7] = new Vector3(extent.max.x, extent.max.y, extent.max.z);
        return answ;
    }
    public static Vector3[] GetAllPoints(PrimitiveMesh parent)
    {
        var ext = parent.ObjectExtent;
        var answ = new Vector3[8];
        answ[0] = new Vector3(ext.min.x, ext.min.y, ext.min.z);
        answ[1] = new Vector3(ext.min.x, ext.min.y, ext.max.z);
        answ[2] = new Vector3(ext.min.x, ext.max.y, ext.min.z);
        answ[3] = new Vector3(ext.min.x, ext.max.y, ext.max.z);
        answ[4] = new Vector3(ext.max.x, ext.min.y, ext.min.z);
        answ[5] = new Vector3(ext.max.x, ext.min.y, ext.max.z);
        answ[6] = new Vector3(ext.max.x, ext.max.y, ext.min.z);
        answ[7] = new Vector3(ext.max.x, ext.max.y, ext.max.z);
        return answ;
    }
    public static Vector3[] GetAllPoints(Polygon poly)
    {
        var ext = poly.PolyExtent;
        var answ = new Vector3[8];
        answ[0] = new Vector3(ext.min.x, ext.min.y, ext.min.z);
        answ[1] = new Vector3(ext.min.x, ext.min.y, ext.max.z);
        answ[2] = new Vector3(ext.min.x, ext.max.y, ext.min.z);
        answ[3] = new Vector3(ext.min.x, ext.max.y, ext.max.z);
        answ[4] = new Vector3(ext.max.x, ext.min.y, ext.min.z);
        answ[5] = new Vector3(ext.max.x, ext.min.y, ext.max.z);
        answ[6] = new Vector3(ext.max.x, ext.max.y, ext.min.z);
        answ[7] = new Vector3(ext.max.x, ext.max.y, ext.max.z);
        return answ;
    }
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
