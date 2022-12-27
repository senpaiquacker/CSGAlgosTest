using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class OperationsAlgorithms
{

    // Start is called before the first frame update
    public static void CalculateSubmeshes(PrimitiveMesh A, PrimitiveMesh B, 
        out (Polygon poly, Polygon.PolyStatus status)[] AMP,
        out (Polygon poly, Polygon.PolyStatus status)[] BMP)
    {

        //Divide(A, B);
        //Divide(B, A);
        //Divide(A, B);
        Divide(A, B);
        AMP = new (Polygon poly, Polygon.PolyStatus status)[A.Polygons.Length];
        BMP = new (Polygon poly, Polygon.PolyStatus status)[B.Polygons.Length];
        /*for(int i = 0; i < A.Polygons.Length; i++)
            AMP[i] = (poly: A.Polygons[i], status: CheckPolyStatus(A.Polygons[i], B));
        for(int i = 0; i < B.Polygons.Length; i++)
            BMP[i] = (poly: B.Polygons[i], status: CheckPolyStatus(B.Polygons[i], A));*/
    }
    public static void Extract(PrimitiveMesh A, PrimitiveMesh B)
    {
        (Polygon poly, Polygon.PolyStatus status)[] AMP;
        (Polygon poly, Polygon.PolyStatus status)[] BMP;
        CalculateSubmeshes(A, B, out AMP, out BMP);
        var Polys = AMP
            .Where(a => a.status == Polygon.PolyStatus.Outside || a.status == Polygon.PolyStatus.BoundaryPointsOut)
            .Concat(BMP.Where(b =>
            {
                if(b.status == Polygon.PolyStatus.Inside)
                {
                    b.poly.FlipPlane();
                    return true;
                }
                return false;
            }))
            .Select(a => a.poly)
            .ToArray();
        A.ExcludePolygons(Polys);
    }

    private static void Divide(PrimitiveMesh A, PrimitiveMesh B)
    {
        var Aex = A.ToGlobal();
        var Bex = B.ToGlobal();
        if (Extent.CheckIntersection(Aex, Bex))
        {
            foreach (var polyA in A.Polygons)
            {
                if (Extent.CheckIntersection(A.ToGlobal(polyA), Bex))
                {
                    foreach (var polyB in B.Polygons)
                    {
                        if (Extent.CheckIntersection(A.ToGlobal(polyA), B.ToGlobal(polyB)))
                        {
                            IEnumerable<float> distances;
                            var inter = GetTwoPolygonsIntersection(polyA, polyB, out distances);
                            if(inter == Intersection.CoplanarIntersection || inter == Intersection.NoIntersection)
                                continue;
                            else
                                SubdivideNonCoplanar(polyA, polyB, distances);
                        }
                    }
                }
            }
        }
    }

    private static Polygon.PolyStatus CheckPolyStatus(Polygon polyA, PrimitiveMesh B)
    {
        foreach(var v in polyA.Vertices)
        {
            if (v.Status == Vertex.VertexStatus.Inside)
                return Polygon.PolyStatus.Inside;
            else if (v.Status == Vertex.VertexStatus.Outside)
                return Polygon.PolyStatus.Outside;
            else if (v.Status == Vertex.VertexStatus.Unknown)
                throw new System.Exception("Unconnected Vertex");
            else continue;
        }
        return CalculateBoundaryPolygon(polyA, B);
        
    }
    private static Polygon.PolyStatus CalculateBoundaryPolygon(Polygon polyA, PrimitiveMesh B)
    {
        var bc = polyA.ToGlobal(polyA.Barycenter);
        var r = new Ray(bc, polyA.PolyPlane.normal);
        bool cast = false;
        (Polygon target, float distance) firstHit = (null, float.PositiveInfinity);
        //while no succesful cast has been made
        while(!cast)
        {
            foreach(var polyB in B.Polygons)
            {
                //find dot product
                var dot = Vector3.Dot(r.direction, polyB.PolyPlane.normal);
                //find the signed distance
                var dist = polyB.GetSignedDistance(bc, true);
                //cast unsuccessful - ray lies on plane
                if(dot == 0 && Mathf.Abs(dist) < AlgoParams.MinDist)
                {
                    cast = false;
                    break;
                }
                //cast is parallel
                else if(dot == 0 && dist > AlgoParams.MinDist)
                    cast = true;
                //point lies on plane, but ray is not
                else if(dot != 0 && Mathf.Abs(dist) < AlgoParams.MinDist)
                {
                    if (polyB.ContainsPoint(polyB.ToLocal(bc), true))
                    {
                        cast = true;
                        firstHit.target = polyB;
                        firstHit.distance = dist;
                        break;
                    }
                    else
                        cast = true;
                }
                //ray is outside of plane
                else if(dot != 0 && dist > AlgoParams.MinDist)
                {
                    var rDist = dist / dot;
                    var point = r.origin + r.direction * rDist;
                    if(rDist < firstHit.distance)
                    {
                        if (polyB.ContainsPoint(point, false))
                        {
                            if (polyB.ContainsPoint(point, true))
                            {
                                cast = false;
                                break;
                            }
                            else
                                firstHit = (polyB, dist);
                        }
                        else cast = true;
                    }
                }
            }
            if (!cast)
                r.direction = Quaternion.Euler(
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f),
                        Random.Range(0, 1f))
                     * r.direction;
        }
        if (firstHit.target == null)
            return Polygon.PolyStatus.Outside;
        var clos_dot = Vector3.Dot(r.direction, firstHit.target.PolyPlane.normal);
        if (firstHit.distance < AlgoParams.MinDist)
        {
            if (clos_dot > 0)
                return Polygon.PolyStatus.BoundaryPointsOut;
            else if (clos_dot < 0)
                return Polygon.PolyStatus.BoundaryPointsIn;
            else
                throw new System.Exception("Didn't perturb ray");
        }
        else if (clos_dot > 0)
            return Polygon.PolyStatus.Inside;
        else if (clos_dot < 0)
            return Polygon.PolyStatus.Outside;
        else
            throw new System.Exception("Something's wrong");
    }
    public static Intersection GetTwoPolygonsIntersection(Polygon polyA, Polygon polyB, out IEnumerable<float> distances, bool IsReversed = false)
    {
        bool isCoplanar = true;
        int k = 0;
        distances = new List<float>(); 
        foreach(var a in polyA.Vertices)
        {
            var dist = polyB.GetSignedDistance(polyA.ToGlobal(a), true);
            if (Mathf.Abs(dist) > AlgoParams.MinDist)
            {
                isCoplanar = false;
                k += (int)Mathf.Sign(dist);
            }
            
            ((List<float>)distances).Add(dist);
        }
        if(Mathf.Abs(k) == polyA.Vertices.Length)
            return 0;
        else if(isCoplanar)
            return Intersection.CoplanarIntersection;
        else
        {
            if(IsReversed)
                return Intersection.PointLineIntersection;
            IEnumerable<float> BtoA;
            var answ = GetTwoPolygonsIntersection(polyB, polyA, out BtoA, true);
            distances = distances.Concat(BtoA);
            return answ;
        }
    }
    public static void SubdivideNonCoplanar(Polygon polyA, Polygon polyB, IEnumerable<float> allDistances)
    {
        var ADistances = new List<float>();
        var BDistances = new List<float>();
        {
            int i = 0;
            foreach (var d in allDistances)
            {
                if (i >= polyA.Vertices.Length)
                    BDistances.Add(d);
                else 
                    ADistances.Add(d);
                i++;
            }
        }
        IntersectionSegment ASeg, BSeg;
        IntersectionSegment.CheckIntersection(polyA, polyB, ADistances, BDistances,  out ASeg, out BSeg);
        IntersectionSegment AinB, BinA;
        var hasInter = IntersectionSegment.CheckIntersection(ASeg, BSeg, out AinB);
        hasInter = IntersectionSegment.CheckIntersection(BSeg, ASeg, out BinA) && hasInter;
        if (!hasInter)
            return;
        DeclareSubdivisionCase(polyA, AinB);
        //DeclareSubdivisionCase(polyB, BinA);
        {
            var i = 0;
            foreach (var dist in allDistances)
            {
                var elem = i >= polyA.Vertices.Length ?
                    polyB.Vertices[i - polyA.Vertices.Length] :
                    polyA.Vertices[i];
                if (elem.Status != Vertex.VertexStatus.Unknown)
                    continue;
                if (dist > AlgoParams.MinDist)
                    elem.MarkVerticeWithUnknown(Vertex.VertexStatus.Outside);
                else
                    elem.MarkVerticeWithUnknown(Vertex.VertexStatus.Inside);
                i++;
            }
        }
    }
    private static void DeclareSubdivisionCase(Polygon polyA, IntersectionSegment AinB)
    {
        switch (AinB.SegmentProperties)
        {
            //1 (1)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Vertex):
                CallCase_1(polyA, AinB);
                break;
            //2 (4)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Vertex):
                CallCase_2(polyA, AinB);
                break;
            //3 (5) and Sym-3 (13)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Edge):
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Vertex):
                CallCase_3(polyA, AinB);
                break;
            //4 (7)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Vertex):
                CallCase_4(polyA, AinB);
                break;
            //5 (8) and Sym-5 (16)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Edge):
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Vertex):
                CallCase_5(polyA, AinB);
                break;
            //6 (9) and Sym-6 (25)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face):
            case (IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Vertex):
                CallCase_6(polyA, AinB);
                break;
            //7 (14)
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Edge):
                CallCase_7(polyA, AinB);
                break;
            //8 (17)
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Edge):
                CallCase_8(polyA, AinB);
                break;
            //9 (18) and Sym-9 (26)
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face):
            case (IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Edge):
                CallCase_9(polyA, AinB);
                break;
            //10 (27)
            case (IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face):
                CallCase_10(polyA, AinB);
                break;
            //others (2, 3, 6, 10, 11, 12, 15, 19, 20, 21, 22, 23, 24)
            default:
                throw new System.Exception("Invalid Polygon");
        }
        polyA.Parent.UpdateMesh();
    }
    #region Intersection Cases
    private static void CallCase_1(Polygon poly, IntersectionSegment seg) => poly.Vertices[seg.PrecedingVertices.first].Status = Vertex.VertexStatus.Boundary;
    private static void CallCase_2(Polygon poly, IntersectionSegment seg)
    {
        poly.Vertices[seg.PrecedingVertices.first].Status = Vertex.VertexStatus.Boundary;
        poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
    }
    private static void CallCase_3(Polygon poly, IntersectionSegment seg)
    {
        Vertex newV;
        var calculatedPoint = seg.SegmentProperties.s == IntersectionSegment.PointType.Edge ?
            seg.LineEquation.p + seg.FirstK * seg.LineEquation.d :
            seg.LineEquation.p + seg.SecondK * seg.LineEquation.d;
        var opposite = seg.SegmentProperties.s == IntersectionSegment.PointType.Edge ? seg.PrecedingVertices.last :
            seg.PrecedingVertices.first;
        poly.Vertices[opposite].Status = Vertex.VertexStatus.Boundary;
        if ((calculatedPoint - poly.ToGlobal(poly.Vertices[seg.PrecedingVertices.first])).sqrMagnitude <= AlgoParams.MinDist * AlgoParams.MinDist)
        {
            poly.Vertices[seg.PrecedingVertices.first].Status = Vertex.VertexStatus.Boundary;
            return;
        }
        if((calculatedPoint - poly.ToGlobal(poly.Vertices[seg.PrecedingVertices.last])).sqrMagnitude <= AlgoParams.MinDist * AlgoParams.MinDist)
        {
            poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
            return;
        }
        poly.Parent.CreateVertex(calculatedPoint, true, out newV, out var ActId);
        poly.Parent.AddDullVertices(newV);
        //No matter, in start is point on edge, or at the end -
        // you connect to last's previous
        // (counts as same as end point if edge in start, and as previous, if edge in end)
        var newPoly = new Polygon(poly.Parent, newV,
            //Previous of lastprevious (to Connect)
            poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
            //Previous of last
            poly.Vertices[seg.PrecedingVertices.last]);
        //find next vertex to a newadded vertex
        var vertexToConnect =
            seg.PrecedingVertices.first != seg.PrecedingVertices.last ?
            poly.Vertices[seg.PrecedingVertices.first] :
            poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length];
        //replace disconnected vertices to a newone
        newV.neighbours.Add(vertexToConnect);
        vertexToConnect.neighbours.Remove(poly.Vertices[seg.PrecedingVertices.last]);
        vertexToConnect.neighbours.Add(newV);
        //Add newcreated Polygon
        poly.Parent.AddPolygon(newPoly);
        poly.Vertices[seg.PrecedingVertices.last] = newV;
        newV.Status = Vertex.VertexStatus.Boundary;
        
    }
    private static void CallCase_4(Polygon poly, IntersectionSegment seg)
    {
        Polygon newP;
        List<Vertex> newPvertices = new List<Vertex>();
        List<Vertex> oldPvertices = new List<Vertex>();
        DivideBySegmentVertices(poly, seg, out newPvertices, out oldPvertices);
        newP = new Polygon(poly.Parent, newPvertices[0], newPvertices[1], newPvertices[2], newPvertices.Skip(3).ToArray());
        poly.Vertices = oldPvertices.ToRing();
        poly.Parent.AddPolygon(newP);
        poly.Vertices[seg.PrecedingVertices.first].Status = 
        poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
        
    }
    private static void CallCase_5(Polygon poly, IntersectionSegment seg)
    {
        int countedPreceeding = seg.SegmentProperties.s == IntersectionSegment.PointType.Edge ? seg.PrecedingVertices.first : seg.PrecedingVertices.last;
        float countedK;
        if(seg.SegmentProperties.s == IntersectionSegment.PointType.Edge)
        {
            countedPreceeding = seg.PrecedingVertices.first;
            countedK = seg.FirstK;
            seg.PrecedingVertices.first++;
        }
        else
        {
            countedPreceeding = seg.PrecedingVertices.last;
            countedK = seg.SecondK;
            seg.PrecedingVertices.last++;
        }
        Vertex newV;
        poly.Parent.CreateVertex(seg.LineEquation.p + countedK * seg.LineEquation.d, true, out newV, out var actId);
        poly.Parent.AddDullVertices(newV);
        var newVerts = poly.Vertices.ToList();
        newVerts.Insert(countedPreceeding, newV);
        poly.Vertices = newVerts.ToRing();
        var newPvertices = new List<Vertex>();
        var oldPvertices = new List<Vertex>();
        DivideBySegmentVertices(poly, seg, out newPvertices, out oldPvertices);
        var newP = new Polygon(poly.Parent, newPvertices[0], newPvertices[1], newPvertices[2], newPvertices.Skip(3).ToArray());
        poly.Vertices = oldPvertices.ToRing();
        poly.Parent.AddPolygon(newP);
        poly.Vertices[seg.PrecedingVertices.first].Status =
        poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
        
    }
    private static void CallCase_6(Polygon poly, IntersectionSegment seg)
    {
        int countedPreceeding;
        float countedK;
        bool isFirst;
        if(seg.SegmentProperties.s == IntersectionSegment.PointType.Face)
        {
            countedPreceeding = seg.PrecedingVertices.first;
            countedK = seg.FirstK;
            isFirst = true;
        }
        else
        {
            countedPreceeding = seg.PrecedingVertices.last;
            countedK = seg.SecondK;
            isFirst = false;
        }
        var cosine = Mathf.Abs(Vector3.Dot(seg.LineEquation.d, poly.Parent.ToGlobal(poly.Vertices[countedPreceeding])));
        Vertex newV;
        poly.Parent.CreateVertex(seg.LineEquation.p + countedK * seg.LineEquation.d, false, out newV, out var actId);
        poly.Parent.AddDullVertices(newV);
        Polygon[] newPs;
        if(cosine > 0.98f)
        {
            //Is on Line
            //There are 4 polygons, so we create 3 new
            newPs = new Polygon[3];

            //Two of them are exactly the same, no matter where face vector is in segment
            newPs[0] = new Polygon(poly.Parent, newV, poly.Vertices[countedPreceeding - 1], poly.Vertices[countedPreceeding]);
            newPs[1] = new Polygon(poly.Parent, newV, poly.Vertices[countedPreceeding], poly.Vertices[(countedPreceeding + 1) % poly.Vertices.Length]);

            //but third and fourth do depend on if segment if v-f-f of f-f-v
            if (isFirst)
            {
                //It means that we're looking at f-f-v polygon
                newPs[2] = new Polygon(poly.Parent,
                    //first three vertices =  i - 1, newV, li + all between li and i - 1 (li;i-1)
                    poly.Vertices[countedPreceeding - 1], newV, poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices
                        .Skip(seg.PrecedingVertices.last + 1)
                        .Take(countedPreceeding - 2 - seg.PrecedingVertices.last)
                        .ToArray());
                poly.Vertices =
                    //the last vertices
                    new [] { newV,  poly.Vertices[(countedPreceeding + 1) % poly.Vertices.Length]}
                    .Concat(poly.Vertices.Skip(countedPreceeding + 1))
                    .Concat(poly.Vertices.Take(seg.PrecedingVertices.last))
                    .ToRing();
            }
            else
            {
                //Now it's v-f-f polygon
                newPs[2] = new Polygon(poly.Parent, newV,
                    poly.Vertices[(countedPreceeding + 1) % poly.Vertices.Length],
                    poly.Vertices[(countedPreceeding + 2) % poly.Vertices.Length],
                    poly.Vertices
                        .Skip(countedPreceeding + 2)
                        .Take(seg.PrecedingVertices.first - countedPreceeding - 2)
                        .ToArray());
                poly.Vertices =
                    new[] { newV, poly.Vertices[seg.PrecedingVertices.first] }
                    .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first))
                    .Concat(poly.Vertices.Take(countedPreceeding - 1))
                    .ToRing();
            }
        }
        else
        {
            //Not on line
            newPs = new Polygon[2];
            newPs[0] = new Polygon(poly.Parent, newV, poly.Vertices[countedPreceeding], poly.Vertices[(countedPreceeding + 1) % poly.Vertices.Length]);
            if(isFirst)
            {
                newPs[1] = new Polygon(poly.Parent, poly.Vertices[seg.PrecedingVertices.first], newV, poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 1)
                        .Skip(seg.PrecedingVertices.last)
                        .ToArray());
                poly.Vertices = new[] { newV, poly.Vertices[(countedPreceeding + 1) % poly.Vertices.Length] }
                    .Concat(poly.Vertices.Skip(countedPreceeding + 1))
                    .Concat(poly.Vertices.Take(seg.PrecedingVertices.last))
                    .ToRing();
            }
            else
            {
                newPs[1] = new Polygon(poly.Parent, poly.Vertices[seg.PrecedingVertices.first], newV, poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 1)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = new[] { newV }
                    .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first - 1))
                    .Concat(poly.Vertices.Take(seg.PrecedingVertices.last))
                    .ToRing();
            }
        }

        poly.Parent.AddPolygon(newPs[0]);
        for(int i = 1; i < newPs.Length; i++)
            poly.Parent.AddPolygon(newPs[i]);
        newV.Status = Vertex.VertexStatus.Boundary;
        if (isFirst)
            poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
        else
            poly.Vertices[seg.PrecedingVertices.first].Status = Vertex.VertexStatus.Boundary;
    }
    private static void CallCase_7(Polygon poly, IntersectionSegment seg)
    {
        if(Mathf.Abs(seg.FirstK - seg.SecondK) < 0.00001f)
        {
            //They are the same point
            Vertex newV;
            poly.Parent.CreateVertex(seg.LineEquation.p + seg.FirstK * seg.LineEquation.d, true, out newV, out var actId);
            poly.Parent.AddDullVertices(newV);
            var newP = new Polygon(poly.Parent, newV, poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                poly.Vertices[seg.PrecedingVertices.last]);
            poly.Vertices = new[] { newV }
                .Concat(poly.Vertices.Skip(seg.PrecedingVertices.last))
                .Concat(poly.Vertices.Take(seg.PrecedingVertices.last))
                .ToRing();

            poly.Parent.AddPolygon(newP);
            newV.Status = Vertex.VertexStatus.Boundary;
        }
        else
        {
            //They Are DifferentPoints
            var newVs = new Vertex[2];
            poly.Parent.CreateVertex(seg.LineEquation.p + seg.FirstK * seg.LineEquation.d, true, out newVs[0], out var ActId);
            poly.Parent.AddDullVertices(newVs[0]);
            poly.Parent.CreateVertex(seg.LineEquation.p + seg.SecondK * seg.LineEquation.d, true, out newVs[1], out var actId);
            poly.Parent.AddDullVertices(newVs[1]);
            var newPs = new Polygon[2];
            newPs[0] = new Polygon(poly.Parent,
                newVs[1],
                poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                poly.Vertices[seg.PrecedingVertices.last]);
            newPs[1] = new Polygon(poly.Parent, 
                newVs[0],
                poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                newVs[1]);
            poly.Parent.AddPolygon(newPs[1]);
            poly.Parent.AddPolygon(newPs[0]);
            foreach (var v in newVs)
                v.Status = Vertex.VertexStatus.Boundary;
        }
    }
    private static void CallCase_8(Polygon poly, IntersectionSegment seg)
    {
        var newVs = new Vertex[2];
        poly.Parent.CreateVertex(seg.LineEquation.p + seg.FirstK * seg.LineEquation.d, true, out newVs[0], out var actId);
        poly.Parent.AddDullVertices(newVs[0]);
        poly.Parent.CreateVertex(seg.LineEquation.p + seg.SecondK * seg.LineEquation.d, true, out newVs[1], out var ActId);
        poly.Parent.AddDullVertices(newVs[1]);
        var newVerts = poly.Vertices.ToList();
        newVerts.Insert(seg.PrecedingVertices.first, newVs[0]);
        seg.PrecedingVertices.first++;
        newVerts.Insert(seg.PrecedingVertices.last, newVs[1]);
        seg.PrecedingVertices.last++;
        poly.Vertices = newVerts.ToRing();
        List<Vertex> newPVertices, oldPVertices;
        DivideBySegmentVertices(poly, seg, out newPVertices, out oldPVertices);
        var newP = new Polygon(poly.Parent, newPVertices[0], newPVertices[1], newPVertices[2], newPVertices.Skip(3).ToArray());
        poly.Vertices = oldPVertices.ToRing();

        poly.Parent.AddPolygon(newP);
        foreach(var v in newVs)
            v.Status = Vertex.VertexStatus.Boundary; 
    }
    private static void CallCase_9(Polygon poly, IntersectionSegment seg)
    {
        var newVs = new Vertex[2];
        poly.Parent.CreateVertex(seg.LineEquation.p + seg.FirstK * seg.LineEquation.d, true, out newVs[0], out var actId);
        poly.Parent.AddDullVertices(newVs[0]);
        poly.Parent.CreateVertex(seg.LineEquation.p + seg.SecondK * seg.LineEquation.d, true, out newVs[1], out var ActId);
        poly.Parent.AddDullVertices(newVs[1]);
        var verts = poly.Vertices.ToList();
        Polygon[] newPs = new Polygon[3];
        if (seg.SegmentProperties.s == IntersectionSegment.PointType.Edge)
        {
            verts.Insert(seg.PrecedingVertices.first, newVs[0]);
            poly.Vertices = verts.ToRing();
            seg.PrecedingVertices.first++;
            seg.PrecedingVertices.last++;
            newPs[0] = new Polygon(poly.Parent, poly.Vertices[seg.PrecedingVertices.last], newVs[1],
                poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length]);
            newPs[1] = new Polygon(poly.Parent, newVs[1], poly.Vertices[seg.PrecedingVertices.last],
                poly.Vertices[seg.PrecedingVertices.last + 1]);
            newPs[2] = new Polygon(poly.Parent,
                newVs[0],
                newVs[1],
                poly.Vertices[seg.PrecedingVertices.last + 1],
                poly.Vertices
                    .Take(seg.PrecedingVertices.first)
                    .Skip(seg.PrecedingVertices.last + 1)
                    .ToArray());
            poly.Vertices = poly.Vertices
                .Skip(seg.PrecedingVertices.first - 1)
                .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                .Concat(new[] { newVs[1] })
                .ToRing();
            poly.Parent.AddPolygon(newPs[2]);
            poly.Parent.AddPolygon(newPs[1]);
            poly.Parent.AddPolygon(newPs[0]);
        }
        else
        {
            verts.Insert(seg.PrecedingVertices.last, newVs[1]);
            poly.Vertices = verts.ToRing();
            seg.PrecedingVertices.last++;
            newPs[0] = new Polygon(poly.Parent, newVs[0], poly.Vertices[seg.PrecedingVertices.first - 1], poly.Vertices[seg.PrecedingVertices.first]);
            newPs[1] = new Polygon(poly.Parent, newVs[0], poly.Vertices[seg.PrecedingVertices.first], poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
            newPs[2] = new Polygon(poly.Parent, poly.Vertices[seg.PrecedingVertices.first - 1], newVs[0], newVs[1],
            poly.Vertices
                    .Take(seg.PrecedingVertices.first - 2)
                    .Skip(seg.PrecedingVertices.last)
                    .ToArray());
            poly.Vertices =
                new[] { newVs[1], newVs[0], poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length] }
                .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first + 1))
                .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                .ToRing();
            poly.Parent.AddPolygon(newPs[2]);
            poly.Parent.AddPolygon(newPs[1]);
            poly.Parent.AddPolygon(newPs[0]);
        }
        newVs[0].Status = Vertex.VertexStatus.Boundary;
        newVs[1].Status = Vertex.VertexStatus.Boundary;
    }
    private static void CallCase_10(Polygon poly, IntersectionSegment seg)
    {
        var isCollinear =
            (first: Mathf.Abs(Vector3.Dot(seg.LineEquation.d, poly.Parent.ToGlobal(poly.Vertices[seg.PrecedingVertices.first]))) > 0.99999f,
             second: Mathf.Abs(Vector3.Dot(seg.LineEquation.d, poly.Parent.ToGlobal(poly.Vertices[seg.PrecedingVertices.last]))) > 0.99999f);
        bool isOnePoint = Mathf.Abs(seg.FirstK - seg.SecondK) < 0.00001f;
        Vertex[] newVs = isOnePoint ? new Vertex[1] : new Vertex[2];
        poly.Parent.CreateVertex(seg.LineEquation.p + seg.LineEquation.d * seg.FirstK, true, out newVs[0], out var actId);
        poly.Parent.AddDullVertices(newVs[0]);
        if (!isOnePoint)
        {
            poly.Parent.CreateVertex(seg.LineEquation.p + seg.LineEquation.d * seg.SecondK, true, out newVs[1], out var ActId);
            poly.Parent.AddDullVertices(newVs[1]);
        }
            Polygon[] newPs;
        switch ((isOnePoint, isCollinear))
        {
            #region Two Points, both continuations are on edges
            case (false, (false, false)):
                newPs = new Polygon[4];
                newPs[0] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first],
                    poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
                newPs[1] = new Polygon(poly.Parent, newVs[0], newVs[1], poly.Vertices[seg.PrecedingVertices.first]);
                newPs[2] = new Polygon(poly.Parent, newVs[1], 
                    poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices[seg.PrecedingVertices.last + 1]);
                newPs[3] = new Polygon(poly.Parent, poly.Vertices[seg.PrecedingVertices.first], newVs[1],
                    poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 1)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = new[] { poly.Vertices[seg.PrecedingVertices.last], newVs[1], newVs[0] }
                                .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first))
                                .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                                .ToRing();
                break;
            #endregion
            #region One Point, both continuations are on edges
            case (true,  (false, false)):
                newPs = new Polygon[3];
                newPs[0] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first],
                    poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
                newPs[1] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices[seg.PrecedingVertices.last + 1]);
                newPs[2] = new Polygon(poly.Parent,
                    poly.Vertices[seg.PrecedingVertices.first],
                    newVs[0],
                    poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 1)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = new[] { poly.Vertices[seg.PrecedingVertices.last], newVs[0], poly.Vertices[seg.PrecedingVertices.first + 1] }
                                .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first + 1))
                                .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                                .ToRing();
                break;
            #endregion
            #region Two Points, first continuation is on edge, second continuation is on vertex
            case (false, (false, true )):
                newPs = new Polygon[4];
                newPs[0] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first],
                    poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
                newPs[1] = new Polygon(poly.Parent, newVs[1],
                    poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                    poly.Vertices[seg.PrecedingVertices.last]);
                newPs[2] = new Polygon(poly.Parent, newVs[1],
                    poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices[seg.PrecedingVertices.last + 1]);
                newPs[3] = new Polygon(poly.Parent, newVs[0], newVs[1],
                    poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = new[] { newVs[1], newVs[0], poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length] }
                        .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first + 1))
                        .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                        .ToRing();
                break;
            #endregion
            #region One Point, first continuation is on edge, second continuation is on vertex
            case (true,  (false, true )):
                newPs = new Polygon[4];
                newPs[0] = new Polygon(poly.Parent, newVs[0],
                   poly.Vertices[seg.PrecedingVertices.first],
                   poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
                newPs[1] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                    poly.Vertices[seg.PrecedingVertices.last]);
                newPs[2] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices[seg.PrecedingVertices.last + 1]);
                newPs[3] = new Polygon(poly.Parent,
                    poly.Vertices[seg.PrecedingVertices.first],
                    newVs[0],
                    poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 1)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = new[] 
                    { 
                        poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                        newVs[0], 
                        poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length] 
                    }
                    .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first + 1))
                    .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 2))
                    .ToRing();
                break;
            #endregion
            #region Two Points, first continuation is on vertex, second continuation is on edge
            case (false, (true , false)):
                newPs = new Polygon[4];
                newPs[0] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first - 1],
                    poly.Vertices[seg.PrecedingVertices.first]);
                newPs[1] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first],
                    poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
                newPs[2] = new Polygon(poly.Parent, newVs[1],
                    poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices[seg.PrecedingVertices.last]);
                newPs[3] = new Polygon(poly.Parent,
                    newVs[0],
                    newVs[1],
                    poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 1)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = new[] { poly.Vertices[seg.PrecedingVertices.last], newVs[1], newVs[0] }
                        .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first))
                        .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                        .ToRing();
                break;
            #endregion
            #region One Point, first continuation is on vertex, second continuation is on edge
            case (true,  (true , false)):
                newPs = new Polygon[4];
                newPs[0] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first - 1],
                    poly.Vertices[seg.PrecedingVertices.first]);
                newPs[1] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first],
                    poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
                newPs[2] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices[seg.PrecedingVertices.last]);
                newPs[3] = new Polygon(poly.Parent,
                    poly.Vertices[seg.PrecedingVertices.first - 1],
                    newVs[0],
                    poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 2)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = new[] { poly.Vertices[seg.PrecedingVertices.last], newVs[0], 
                poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length] }
                        .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first + 1))
                        .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                        .ToRing();
                break;
            #endregion
            #region Two Points, both continuations are on vertices
            case (false, (true , true )):
                newPs = new Polygon[5];
                newPs[0] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first - 1],
                    poly.Vertices[seg.PrecedingVertices.first]);
                newPs[1] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first],
                    poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
                newPs[2] = new Polygon(poly.Parent, newVs[1],
                    poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                    poly.Vertices[seg.PrecedingVertices.last]);
                newPs[3] = new Polygon(poly.Parent, newVs[1],
                    poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices[seg.PrecedingVertices.last + 1]);
                newPs[4] = new Polygon(poly.Parent, newVs[0], newVs[1],
                    poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 1)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = new[] { newVs[1], newVs[0], poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length] }
                        .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first + 1))
                        .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                        .ToRing();
                break;
            #endregion
            #region One Point, both continuations are on vertices
            case (true,  (true , true )):
                newPs = new Polygon[5];
                newPs[0] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first - 1],
                    poly.Vertices[seg.PrecedingVertices.first]);
                newPs[1] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.first],
                    poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length]);
                newPs[2] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                    poly.Vertices[seg.PrecedingVertices.last]);
                newPs[3] = new Polygon(poly.Parent, newVs[0],
                    poly.Vertices[seg.PrecedingVertices.last],
                    poly.Vertices[seg.PrecedingVertices.last + 1]);
                newPs[4] = new Polygon(poly.Parent, poly.Vertices[seg.PrecedingVertices.first - 1], newVs[0],
                    poly.Vertices[seg.PrecedingVertices.last + 1],
                    poly.Vertices
                        .Take(seg.PrecedingVertices.first - 2)
                        .Skip(seg.PrecedingVertices.last + 1)
                        .ToArray());
                poly.Vertices = 
                new[] 
                { 
                    poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length], 
                    newVs[0], 
                    poly.Vertices[(seg.PrecedingVertices.first + 1) % poly.Vertices.Length] 
                }
                    .Concat(poly.Vertices.Skip(seg.PrecedingVertices.first + 1))
                    .Concat(poly.Vertices.Take(seg.PrecedingVertices.last - 1))
                    .ToRing();
                break;
                #endregion
        }

        poly.Parent.AddPolygon(newPs[0]);
        for(int i = 1; i < newPs.Length; i++)
            poly.Parent.AddPolygon(newPs[i]);
        for (int i = 0; i < newVs.Length; i++)
            newVs[i].Status = Vertex.VertexStatus.Boundary;
    }
    #endregion
    private static void DivideBySegmentVertices(Polygon poly, IntersectionSegment seg, 
        out List<Vertex> newPvertices, out List<Vertex> oldPvertices)
    {
        bool isStarted = false;
        newPvertices = new List<Vertex>();
        oldPvertices = new List<Vertex>();
        for (int i = seg.PrecedingVertices.last; i < poly.Vertices.Length; i++)
        {
            if (i == seg.PrecedingVertices.first ||
               i == seg.PrecedingVertices.last)
            {
                if (!isStarted)
                {
                    if (newPvertices.Count == 0)
                    {
                        newPvertices.Add(poly.Vertices[i]);
                        isStarted = true;
                    }
                    else
                    {
                        oldPvertices.Add(poly.Vertices[i]);
                        isStarted = true;
                    }
                }
                else
                {
                    if (oldPvertices.Count == 0)
                    {
                        newPvertices.Add(poly.Vertices[i]);
                        isStarted = false;
                    }
                    else
                    {
                        isStarted = false;
                        break;
                    }
                }
            }
            else
            {
                if (isStarted)
                {
                    if (oldPvertices.Count == 0)
                        newPvertices.Add(poly.Vertices[i]);
                    else oldPvertices.Add(poly.Vertices[i]);
                }
            }
            if (i == poly.Vertices.Length - 1 && oldPvertices.Count + newPvertices.Count - 2 < poly.Vertices.Length)
                i = -1;
        }
    }
    private static bool IsMoreThanThree(Polygon poly)
    {
        return poly.Vertices.Length > 3;
    }
    public enum Intersection
    {
        NoIntersection = 0,
        PointLineIntersection = 5,
        CoplanarIntersection = 10
    }
}
