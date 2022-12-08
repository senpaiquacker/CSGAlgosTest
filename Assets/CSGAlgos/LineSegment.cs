using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IntersectionSegment
{
    public (Vector3 p, Vector3 d) LineEquation;
    public enum PointType
    {
        None = 0,
        Vertex = 1,
        Edge = 2,
        Face = 3
    }
    public float FirstK;
    public float SecondK;
    public float MiddlePointK => (FirstK + SecondK) / 2;
    public (PointType s, PointType m, PointType l) SegmentProperties;
    public (int first, int last) PrecedingVertices;

    private IntersectionSegment() { }
    public static void CheckIntersection(
        Polygon polyA, Polygon polyB, 
        IEnumerable<float> ADistances, IEnumerable<float> BDistances,
        out IntersectionSegment AIntersection, out IntersectionSegment BIntersection)
    {
        AIntersection = new IntersectionSegment();
        BIntersection = new IntersectionSegment();
        var a = polyA.ToGlobal();
        var b = polyB.ToGlobal();
        var d = Vector3.Cross(a.normal, b.normal).normalized;
        if(d == Vector3.zero) return;
        //assume z = 0
        var p = new Vector3(

            (a.distance * b.normal.y - b.distance * a.normal.y),
            (b.distance * a.normal.x - a.distance * b.normal.x),
            0) 

            / (b.normal.x * a.normal.y - a.normal.x * b.normal.y);

        p = (!float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z)) ? p :  new Vector3(

            (a.distance * b.normal.z - b.distance * a.normal.z),
            0,
            (b.distance * a.normal.x - a.distance * b.normal.x)) 

            / (b.normal.x * a.normal.z - a.normal.x * b.normal.z);

        p = (!float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z)) ? p : new Vector3(

            0,
            (a.distance * b.normal.z - b.distance * a.normal.z),
            (b.distance * a.normal.y - a.distance * b.normal.y))

            / (b.normal.y * a.normal.z - a.normal.y - b.normal.z);

        AIntersection.LineEquation = (p, d);
        BIntersection.LineEquation = (p, d);
        var APoints = GetLineSegment(AIntersection.LineEquation, polyA, ADistances, out AIntersection.PrecedingVertices, 
            out AIntersection.SegmentProperties);
        var BPoints = GetLineSegment(BIntersection.LineEquation, polyB, BDistances, out BIntersection.PrecedingVertices,
            out BIntersection.SegmentProperties);
        AIntersection.FirstK = APoints.k1;
        AIntersection.SecondK = APoints.k2;
        BIntersection.FirstK = BPoints.k1;
        BIntersection.SecondK = BPoints.k2;
    }
    private static (float k1, float k2) GetLineSegment(
        (Vector3 p, Vector3 d) line, Polygon polygon, IEnumerable<float> distances, 
        out (int first, int last) precedingVertices,
        out (PointType first, PointType middle, PointType last) segmentType)
    {
        (float k1, float k2) points = (0, 0);
        precedingVertices = (-1, -1);
        segmentType = (0, 0, 0);

        var dists = distances.ToArray();
        bool isSecond = false;
        for (int i = 0; i < polygon.Vertices.Length; i++)
        {
            if(dists[i] == 0)
            {
                if (!isSecond)
                {
                    isSecond = true;
                    precedingVertices.first = i;
                    segmentType.first = PointType.Vertex;
                }
                else
                {
                    precedingVertices.last = i;
                    segmentType.last = PointType.Vertex;
                    if(segmentType.first == PointType.Vertex)
                    {
                        if (Mathf.Abs(precedingVertices.first - precedingVertices.last) == 1 ||
                            Mathf.Abs(precedingVertices.first + polygon.Vertices.Length - precedingVertices.last) == 1 ||
                            Mathf.Abs(precedingVertices.last + polygon.Vertices.Length - precedingVertices.first) == 1)
                            segmentType.middle = PointType.Edge;
                        else
                            segmentType.middle = PointType.Face;
                    }
                    else if(segmentType.first == PointType.Edge)
                        segmentType.middle = PointType.Face;
                    break;
                }
            }
            else if(Mathf.Sign(dists[i]) != Mathf.Sign(dists[(i + 1) % dists.Length]) && dists[(i + 1) % dists.Length] != 0)
            {
                if(!isSecond)
                {
                    isSecond = true;
                    precedingVertices.first = i;
                    segmentType.first = PointType.Edge;
                }
                else
                {
                    precedingVertices.last = i;
                    segmentType.last = PointType.Edge;
                    if (segmentType.first == PointType.Vertex)
                        segmentType.middle = PointType.Face;
                    else if (segmentType.first == PointType.Edge)
                        segmentType.middle = PointType.Face;
                    break;
                }
            }
        }
        if (precedingVertices.last == -1 && segmentType.first == PointType.Vertex)
        {
            precedingVertices.last = precedingVertices.first;
            segmentType.middle = segmentType.last = PointType.Vertex;
        }
        if (segmentType.first == PointType.None ||
           segmentType.middle == PointType.None ||
           segmentType.last == PointType.None)
            throw new System.Exception("Invalid Polygon");
        points.k1 = segmentType.first == PointType.Vertex ? Vector3.Dot(line.p + line.d, polygon.ToGlobal(polygon.Vertices[precedingVertices.first]) - line.p) :
            GetLineEdgeIntersection(line,
                (polygon.ToGlobal(polygon.Vertices[precedingVertices.first]), 
                polygon.ToGlobal(polygon.Vertices[(precedingVertices.first + 1) % polygon.Vertices.Length])),
                (dists[precedingVertices.first], dists[(precedingVertices.first + 1) % polygon.Vertices.Length]));
        points.k2 = segmentType.last == PointType.Vertex ?
            Vector3.Dot(line.p + line.d, polygon.ToGlobal(polygon.Vertices[precedingVertices.last]) - line.p) :
            GetLineEdgeIntersection(line,
            (polygon.ToGlobal(polygon.Vertices[precedingVertices.last]),
            polygon.ToGlobal(polygon.Vertices[(precedingVertices.last + 1) % polygon.Vertices.Length])),
            (dists[precedingVertices.last], dists[(precedingVertices.last + 1) % polygon.Vertices.Length]));
        if(points.k2 < points.k1)
        {
            var k3 = points.k1;
            points.k1 = points.k2;
            points.k2 = k3;
            var i3 = precedingVertices.first;
            precedingVertices.first = precedingVertices.last;
            precedingVertices.last = i3;
        }
        return points;
    }
    private static float GetLineEdgeIntersection((Vector3 p, Vector3 d) line, (Vector3 x1, Vector3 x2) edge, (float d1, float d2) distancesToLine)
    {
        var eLine = (p: edge.x1, d: edge.x2 - edge.x1);
        var sumDist = Mathf.Abs(distancesToLine.d1) + Mathf.Abs(distancesToLine.d2);
        var aok = Mathf.Abs(distancesToLine.d1) / sumDist;
        var ao = eLine.d * aok + eLine.p;
        return (ao - line.p).magnitude * Mathf.Sign(Vector3.Dot(ao - line.p, line.d));
    }
    public static IntersectionSegment Copy(IntersectionSegment seg)
    {
        var k = new IntersectionSegment();
        k.FirstK = seg.FirstK;
        k.SecondK = seg.SecondK;
        k.LineEquation = (seg.LineEquation.p, seg.LineEquation.d);
        k.PrecedingVertices = (seg.PrecedingVertices.first, seg.PrecedingVertices.last);
        k.SegmentProperties = (seg.SegmentProperties.s, seg.SegmentProperties.m, seg.SegmentProperties.l);
        return k;
    }
    public static bool CheckIntersection(IntersectionSegment segA, IntersectionSegment segB, out IntersectionSegment intersection)
    {
        intersection = Copy(segA);
        var intersectionsType = (
            segB.CheckPointInsideSegment(segA.SecondK),
            segB.CheckPointInsideSegment(segA.FirstK),
            segA.CheckPointInsideSegment(segB.SecondK),
            segA.CheckPointInsideSegment(segB.FirstK)
            );
        bool hasInter;
        switch(intersectionsType)
        {
            case (false, false, false, false):
                hasInter = false;
                break;
            case (false, false, true, true):
                intersection.FirstK = segB.FirstK;
                intersection.SecondK = segB.SecondK;
                intersection.SegmentProperties.s = intersection.SegmentProperties.l = intersection.SegmentProperties.m;
                hasInter = true;
                break;
            case (false, true, true, false):
            case (false, true, true, true):
                intersection.SecondK = segB.SecondK;
                intersection.SegmentProperties.l = intersection.SegmentProperties.m;
                hasInter = true;
                break;
            case (true, false, false, true):
            case (true, false, true, true):
                intersection.FirstK = segB.FirstK;
                intersection.SegmentProperties.s = intersection.SegmentProperties.m;
                hasInter = true;
                break;
            case (true, true, false, false):
            case (true, true, false, true):
            case (true, true, true, false):
            case (true, true, true, true):
                hasInter = true;
                break;
            default:
                throw new System.Exception("What? How is this even possible?");
        }
        SwapByVerticesOrder(intersection);
        return hasInter;
    }
    private bool CheckPointInsideSegment(float pointK)
    {
        return pointK >= FirstK && pointK <= SecondK;
    }
    //Method swaps intersection order as in descending preceding vertices id order so that the 0-point of poly vertices is always above both of them logically
    private static IntersectionSegment SwapByVerticesOrder(IntersectionSegment intersection)
    {
        if (intersection.PrecedingVertices.first < intersection.PrecedingVertices.last)
        {
            var k3 = intersection.FirstK;
            intersection.FirstK = intersection.SecondK;
            intersection.SecondK = k3;
            var v3 = intersection.PrecedingVertices.first;
            intersection.PrecedingVertices.first = intersection.PrecedingVertices.last;
            intersection.PrecedingVertices.last = v3;
            var t3 = intersection.SegmentProperties.s;
            intersection.SegmentProperties.s = intersection.SegmentProperties.l;
            intersection.SegmentProperties.l = t3;
        }
        return intersection;
    }
    public override string ToString()
    {
        return $"{{LineEq: (p:{LineEquation.p.ToString("F4")}; d:{LineEquation.d.ToString("F4")}) - Points({FirstK}, {SecondK})}}";
    }
}

