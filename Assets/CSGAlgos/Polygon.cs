using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Polygon
{
    public PrimitiveMesh Parent { get; private set; }
    public Ring<Vertex> Vertices;
    public Plane PolyPlane { get; private set; }
    public (Vector3 min, Vector3 max) PolyExtent 
    { 
        get =>
        (
            min: new Vector3
            (
                Vertices[extentIds.min.x].LocalPosition.x,
                Vertices[extentIds.min.y].LocalPosition.y,
                Vertices[extentIds.min.z].LocalPosition.z
            ), 
            max: new Vector3
            (
                Vertices[extentIds.max.x].LocalPosition.x,
                Vertices[extentIds.max.y].LocalPosition.y,
                Vertices[extentIds.max.z].LocalPosition.z
            )
        );
    }
    public Vector3 Barycenter { 
        get
        {
            Vector3 sum = Vector3.zero;
            foreach(var v in Vertices)
                sum += v.LocalPosition;
            sum /= Vertices.Length;
            return sum;
        } 
    }

    private ((int x, int y, int z) min, (int x, int y, int z) max) extentIds;

    public bool ContainsPoint(Vector3 point, bool includeEdges)
    {
        PolySide prevSide = PolySide.None;
        for(int i = 0; i < Vertices.Length; i++)
        {
            var edge = (Vertices[i].LocalPosition, Vertices[(i + 1) % Vertices.Length].LocalPosition);
            var eDir = (edge.Item2 - edge.Item1);
            var pDir = (point - edge.Item1);
            PolySide side = Vector3.Dot(Vector3.Cross(eDir, pDir), PolyPlane.normal) < 0 ? PolySide.Left :
                Vector3.Dot(Vector3.Cross(eDir, pDir), PolyPlane.normal) > 0 ? PolySide.Right : PolySide.None;
            if (side == 0)
            {
                if ((Vector3.Dot(eDir, pDir) > 0 && pDir.magnitude / eDir.magnitude <= 1 || point == edge.Item1) && includeEdges)
                    return true;
                else return false;
            }
            else if (prevSide == 0)
                prevSide = side;
            else if (prevSide != side)
                return false;
        }
        return true;
    }
    private enum PolySide
    {
        None = 0,
        Left = 1,
        Right = 2
    }
    public Polygon(PrimitiveMesh parent, Vertex f, Vertex s, Vertex t, params Vertex[] additional)
    {
        Parent = parent;
        #region Plane Equation
        var p1 = f.LocalPosition;
        var p2 = s.LocalPosition;
        var p3 = t.LocalPosition;
        /*var A = (p2.y - p1.y) * (p3.z - p1.z) - (p3.y - p1.y) * (p2.z - p1.z);
        var B = (p3.x - p1.x) * (p2.z - p1.z) - (p2.x - p1.x) * (p3.z - p1.z);
        var C = (p2.x - p1.x) * (p3.y - p1.y) - (p3.x - p1.x) * (p2.y - p1.y);*/
        PolyPlane = new Plane(p1, p2, p3);
        #endregion
        Vertices = additional.Concat(new Vertex[] { f, s, t }).ToRing();
        #region Handle All Vertices
        extentIds = Extent.UpdateExtent(Vertices.Select(a => a.LocalPosition));
        foreach (var v in Vertices)
        {
            #region Additional Vertices Check
            var dist = Mathf.Abs(GetSignedDistance(v.LocalPosition, false));
            if (dist > AlgoParams.MinDist)
                throw new System.ArgumentException($"Polygon is not Planar. {v.LocalPosition} is out of plane");
            #endregion
            #region Assign Neighbours to Vertex
            v.neighbours.AddRange(Vertices.Except(new[] {v}).ToList());
            v.neighbours = v.neighbours.Distinct().ToList();
            #endregion
        }
        #endregion
    }
    public float GetSignedDistance(Vector3 point, bool isGlobal)
    {
        var plane = isGlobal ? ToGlobal() : PolyPlane;
        return plane.GetDistanceToPoint(point);
    }
    public Plane ToGlobal() => Parent.ToGlobal(PolyPlane, ToGlobal(Vertices[0]));
    public Vector3 ToGlobal(Vertex vert) => Parent.ToGlobal(vert);
    public Vector3 ToGlobal(Vector3 pos) => Parent.ToGlobal(pos);
    public Vector3 ToLocal(Vector3 global) => Parent.ToLocal(global);

    public enum PolyStatus
    {
        Unknown = 0,
        BoundaryPointsIn = 1,
        BoundaryPointsOut = 2,
        Inside = 10,
        Outside = 20,
    }
    public void FlipPlane()
    {
        Vertices = Vertices.Reverse().ToRing();
        PolyPlane = PolyPlane.flipped;
    }
    public override string ToString()
    {
        return $"({PolyPlane.normal.ToString("F4")},{PolyPlane.distance}";
    }
    public void ChangeParent(PrimitiveMesh newParent)
    {
        for(int i = 0; i < Vertices.Length; i++)
        {
            var v = Vertices[i];
            var k = newParent
                .Vertices
                .FirstOrDefault(a => 
                    (a.LocalPosition - newParent.ToLocal(ToGlobal(v))).sqrMagnitude <= AlgoParams.MinDist * AlgoParams.MinDist);
            if (k != default)
            {
                k.neighbours.Concat(Vertices.Except(new[] { v }));
                Vertices[i] = k;
            }
            else
            {
                var newV = Vertex.CreateVertex(newParent, newParent.ToLocal(ToGlobal(v)));
                newParent.AddDullVertices(newV);
            }
        }
        Parent = newParent;
    }
}
