using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class DebugAnimator : MonoBehaviour
{
    
    private Dictionary<object, GameObject> DrawedObjects;
    [SerializeField]
    private List<GameObject> DebugObjects;
    [SerializeField]
    private KeyCode KeyToNextStep;
    private static bool IsKeyPressed;
    private void Awake()
    {
        DrawedObjects = new Dictionary<object, GameObject>();
        DebugObjects = new List<GameObject>();
    }
    public static DebugAnimator GlobalAnimator
    {
        get => Camera.main.GetComponent<DebugAnimator>();
    }
    public static IEnumerator WaitForKeyToPush()
    {
        while (!IsKeyPressed)
        {
            IsKeyPressed = Input.GetKeyDown(GlobalAnimator.KeyToNextStep);
            if (IsKeyPressed)
                break;
            yield return new WaitForEndOfFrame();
        }
        IsKeyPressed = false;
    }
    // Start is called before the first frame update
    #region Drawing Methods
    #region private (Real) Draw Methods
    private static GameObject DebugDrawPolygon(Polygon poly)
    {
        if (GlobalAnimator.DrawedObjects.ContainsKey(poly))
            return GlobalAnimator.DrawedObjects[poly];
        var polygon = new GameObject();
        polygon.name = poly.Parent.name + "'s polygon";
        var mesh = polygon.AddComponent<MeshFilter>();
        mesh.mesh = new Mesh();
        mesh.mesh.vertices = 
            poly
            .Vertices
            .Select(a => a.LocalPosition)
            .ToArray();
        {
            int i = 0;
            mesh.mesh.triangles = 
                poly
                .Vertices
                .Select(a => i++)
                .ToArray();
        }
        var material = polygon.AddComponent<MeshRenderer>();
        material.material = new Material(Shader.Find("Specular"));
        material.material.color = Color.green;
        polygon.transform.position = poly.Parent.transform.position;
        polygon.transform.rotation = poly.Parent.transform.rotation;
        polygon.transform.localScale = poly.Parent.transform.localScale;
        GlobalAnimator.DrawedObjects.Add(poly, polygon);
        Camera.main.Render();
        GlobalAnimator.DebugObjects.Add(polygon);
        return polygon;
    }
    private static GameObject DebugDrawPolygon(Polygon poly, Color color)
    {
        var p = DebugDrawPolygon(poly);
        p.GetComponent<MeshRenderer>().material.color = color;
        return p;
    }
    private static GameObject DebugDrawLine((Vector3 p, Vector3 d) lineEquation, float firstBorder = -1000f, float secondBorder = 1000f)
    {
        if (GlobalAnimator.DrawedObjects.ContainsKey((lineEquation, firstBorder, secondBorder)))
            return GlobalAnimator.DrawedObjects[(lineEquation,firstBorder, secondBorder)];
        var line = new GameObject();
        line.name = "IntersectionLine";
        var renderer = line.AddComponent<LineRenderer>();
        renderer.widthMultiplier = 0.04f;
        renderer.material = new Material(Shader.Find("Specular"));
        renderer.material.color = Color.blue;
        renderer.positionCount = 3;
        renderer.SetPosition(0, lineEquation.p + firstBorder * lineEquation.d);
        renderer.SetPosition(1, lineEquation.p + (firstBorder + secondBorder) / 2 * lineEquation.d);
        renderer.SetPosition(2, lineEquation.p + secondBorder * lineEquation.d);
        GlobalAnimator.DrawedObjects.Add((lineEquation, firstBorder, secondBorder), line);
        Camera.main.Render();
        GlobalAnimator.DebugObjects.Add(line);
        return line;
    }
    private static GameObject DebugDrawLine((Vector3 p, Vector3 d) lineEquation, (float first, float second) borders)
    {
        var line = DebugDrawLine(lineEquation, firstBorder: borders.first, secondBorder: borders.second);
        var render = line.GetComponent<LineRenderer>();
        render.material.color = new Color(81, 114, 92);
        render.widthMultiplier *= 2;
        return line;
    }
    private static GameObject DebugDrawPoint(Vector3 position, string parentName = "")
    {
        foreach(var obj in GlobalAnimator.DrawedObjects.Keys)
        {
            if(obj is Vector3)
            {
                if(((Vector3)obj - position).magnitude <= AlgoParams.MinDist)
                    return GlobalAnimator.DrawedObjects[obj];
            }
        }
        var point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        point.name = parentName + " Point at position " + position.ToString("F4");
        point.GetComponent<Collider>().enabled = false;
        point.transform.localScale = Vector3.one * 0.1f;
        var mat = point.GetComponent<MeshRenderer>();
        mat.material.color = Color.white;
        point.transform.position = position;
        GlobalAnimator.DrawedObjects.Add(position, point);
        Camera.main.Render();
        GlobalAnimator.DebugObjects.Add(point);
        return point;
    }
    #endregion
    #region public Draw Methods
    public static IEnumerator DrawPolygon(Polygon poly)
    {
        DebugDrawPolygon(poly);
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DrawPolygon(Polygon poly, Color color)
    {
        DebugDrawPolygon(poly, color);
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DrawPoint(Vector3 position)
    {
        Debug.Log("Drawing Point on " + position.ToString("F4"));
        DebugDrawPoint(position);
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DrawPoint(Vector3 position, string parentName)
    {
        Debug.Log("Drawing Point on " + position.ToString("F4") + " with name " + parentName);
        DebugDrawPoint(position, parentName);
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DrawLineSegment(IntersectionSegment seg, string segName = "")
    {
        Debug.Log("Drawing line Segment with line " + seg.LineEquation + "and Type" + 
            seg.SegmentProperties.s.ToString()[0] + "-" + seg.SegmentProperties.m.ToString()[0] + "-" + seg.SegmentProperties.l.ToString()[0]);
        yield return DrawLine(seg.LineEquation, (seg.FirstK, seg.SecondK));
        if (segName == "")
        {
            yield return DrawPoint(seg.FirstPoint);
            yield return DrawPoint(seg.SecondPoint);
        }
        else
        {
            yield return DrawPoint(seg.FirstPoint, segName);
            yield return DrawPoint(seg.SecondPoint, segName);
        }
    }
    public static IEnumerator DrawLine((Vector3 p, Vector3 d) lineEquation)
    {
        DebugAnimator.DebugDrawLine(lineEquation);
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DrawLine((Vector3 p, Vector3 d) lineEquation, (float first, float second) borders)
    {
        DebugDrawLine(lineEquation, borders);
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator RecolorPoint(Vector3 position, Color color)
    {
        foreach (var obj in GlobalAnimator.DrawedObjects.Keys)
        {
            if (obj is Vector3)
            {
                if (((Vector3)obj - position).magnitude <= AlgoParams.MinDist)
                {
                    var val = GlobalAnimator.DrawedObjects[obj];
                    var mat = val.GetComponent<MeshRenderer>();
                    mat.material.color = color;
                    break;
                }
            }
        }
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DrawExtent(PrimitiveMesh mesh)
    {
        foreach (var p in Extent.GetAllPoints(mesh))
            DebugDrawPoint(p);
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DrawExtent(Polygon poly)
    {
        foreach (var p in Extent.GetAllPoints(poly))
            DebugDrawPoint(p);
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    #endregion
    #region DestroyMethods
    public static IEnumerator DestroyPolygon(Polygon poly, bool skipTime = false)
    {
        if (GlobalAnimator.DrawedObjects.ContainsKey(poly))
        {
            var val = GlobalAnimator.DrawedObjects[poly];
            GameObject.DestroyImmediate(val);

            GlobalAnimator.DrawedObjects.Remove(poly);
            GlobalAnimator.DebugObjects.Remove(val);
        }
        if (skipTime)
            yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DestroyLine((Vector3 p, Vector3 d) lineEquation, bool skipTime = false)
    {
        if (GlobalAnimator.DrawedObjects.ContainsKey((lineEquation, -1000f, 1000f)))
        {
            var val = GlobalAnimator.DrawedObjects[(lineEquation, -1000f, 1000f)];
            GameObject.DestroyImmediate(val);
            GlobalAnimator.DrawedObjects.Remove((lineEquation, -1000f, 1000f));
            GlobalAnimator.DebugObjects.Remove(val);
        }
        if (skipTime)
            yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    private static IEnumerator DestroyLine((Vector3 p, Vector3 d) lineEquation, (float first, float second) borders, bool skipTime = false)
    {
        if (GlobalAnimator.DrawedObjects.ContainsKey((lineEquation, borders.first, borders.second)))
        {
            var val = GlobalAnimator.DrawedObjects[(lineEquation, borders.first, borders.second)];
            GameObject.DestroyImmediate(val);
            GlobalAnimator.DrawedObjects.Remove((lineEquation, borders.first, borders.second));
            GlobalAnimator.DebugObjects.Remove(val);
        }
        if (skipTime)
            yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DestroyPoint(Vector3 position, bool skipTime = false)
    {
        foreach(var obj in GlobalAnimator.DrawedObjects.Keys)
        {
            if(obj is Vector3)
            {
                if(((Vector3)obj - position).magnitude < AlgoParams.MinDist)
                {
                    var val = GlobalAnimator.DrawedObjects[obj];
                    DestroyImmediate(val);
                    GlobalAnimator.DrawedObjects.Remove(obj);
                    GlobalAnimator.DebugObjects.Remove(val);
                    break;
                }
            }
            
        }
        if(skipTime)
            yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    public static IEnumerator DestroyLineSegment(IntersectionSegment seg)
    {
        yield return DestroyLine(seg.LineEquation, (seg.FirstK, seg.SecondK));
        yield return DestroyPoint(seg.FirstPoint);
        yield return DestroyPoint(seg.SecondPoint);
    }
    public static IEnumerator DestroyExtent(PrimitiveMesh mesh)
    {
        foreach(var p in Extent.GetAllPoints(mesh))
        {
            foreach(var obj in GlobalAnimator.DrawedObjects.Keys)
            {
                if ((p - (Vector3)obj).sqrMagnitude < AlgoParams.MinDist * AlgoParams.MinDist)
                {
                    var val = GlobalAnimator.DrawedObjects[obj];
                    DestroyImmediate(val);
                    GlobalAnimator.DrawedObjects.Remove(obj);
                    GlobalAnimator.DebugObjects.Remove(val);
                    break;
                }
            }
        }
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    #endregion
    #endregion
    public static void Extract(PrimitiveMesh A, PrimitiveMesh B)
    {
        (Polygon poly, Polygon.PolyStatus status)[] AMP;
        (Polygon poly, Polygon.PolyStatus status)[] BMP;
        CalculateSubmeshes(A, B, out AMP, out BMP);
    }
    public static void CalculateSubmeshes(PrimitiveMesh A, PrimitiveMesh B,
       out (Polygon poly, Polygon.PolyStatus status)[] AMP,
       out (Polygon poly, Polygon.PolyStatus status)[] BMP)
    {

        //Divide(A, B);
        //Divide(B, A);
        //Divide(A, B);
        Camera.main.GetComponent<DebugAnimator>().StartCoroutine(Divide(A, B));
        AMP = new (Polygon poly, Polygon.PolyStatus status)[A.Polygons.Length];
        BMP = new (Polygon poly, Polygon.PolyStatus status)[B.Polygons.Length];
        /*for(int i = 0; i < A.Polygons.Length; i++)
            AMP[i] = (poly: A.Polygons[i], status: CheckPolyStatus(A.Polygons[i], B));
        for(int i = 0; i < B.Polygons.Length; i++)
            BMP[i] = (poly: B.Polygons[i], status: CheckPolyStatus(B.Polygons[i], A));*/
    }
    private static IEnumerator Divide(PrimitiveMesh A, PrimitiveMesh B)
    {
        foreach (var polyA in A.Polygons)
        {
            yield return DrawPolygon(polyA);
            yield return DestroyPolygon(polyA);
        }
        foreach (var polyB in B.Polygons)
        {
            yield return DrawPolygon(polyB);
            yield return DestroyPolygon(polyB);
        }
        yield return DrawExtent(A);
        yield return DrawExtent(B);
        yield return DestroyExtent(A);
        yield return DestroyExtent(B);
        if (Extent.CheckIntersection(A.ObjectExtent, B.ObjectExtent))
        {
            foreach (var polyA in A.Polygons)
            {
                yield return DrawPolygon(polyA);
                if (Extent.CheckIntersection(A.ToGlobal(polyA), B.ObjectExtent))
                {
                    foreach (var polyB in B.Polygons)
                    {
                        yield return DrawPolygon(polyB);
                        if (Extent.CheckIntersection(A.ToGlobal(polyA), B.ToGlobal(polyB)))
                        {
                            IEnumerable<float> distances;
                            var inter = OperationsAlgorithms.GetTwoPolygonsIntersection(polyA, polyB, out distances);
                            if (inter == OperationsAlgorithms.Intersection.CoplanarIntersection ||
                                inter == OperationsAlgorithms.Intersection.NoIntersection)
                            {
                                yield return DestroyPolygon(polyB);
                                continue;
                            }
                            else
                                yield return SubdivideNonCoplanar(polyA, polyB, distances);
                        }
                        yield return DestroyPolygon(polyB);
                    }
                }
                yield return DestroyPolygon(polyA);
            }
        }

    }
    public static IEnumerator SubdivideNonCoplanar(Polygon polyA, Polygon polyB, IEnumerable<float> allDistances)
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
        IntersectionSegment.CheckIntersection(polyA, polyB, ADistances, BDistances, out ASeg, out BSeg);
        yield return DrawLine(ASeg.LineEquation);
        yield return DrawLineSegment(ASeg, "PolyA Segment");
        yield return DrawLineSegment(BSeg, "PolyB Segment");
        IntersectionSegment AinB, BinA;
        var hasInter = IntersectionSegment.CheckIntersection(ASeg, BSeg, out AinB);
        yield return DestroyLineSegment(ASeg);
        yield return DestroyLineSegment(BSeg);
        yield return DrawLineSegment(AinB, "AinB");
        Debug.Log("Continuing?" + (hasInter ? " True" : " False"));
        //hasInter = IntersectionSegment.CheckIntersection(BSeg, ASeg, out BinA) && hasInter;
        
        if (hasInter)
        {

            yield return DeclareSubdivisionCase(polyA, AinB);
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
        yield return DestroyLine(ASeg.LineEquation);
        yield return DestroyLineSegment(AinB);
    }
    public static IEnumerator DeclareSubdivisionCase(Polygon poly, IntersectionSegment AinB)
    {
        switch (AinB.SegmentProperties)
        {
            //1 (1)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Vertex):
                Debug.Log("It's Case 1 v-v-v");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_1(poly, AinB);
                break;
            //2 (4)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Vertex):
                Debug.Log("It's Case 4 v-e-v");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_2(poly, AinB);
                break;
            //3 (5) and Sym-3 (13)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Edge):
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Vertex):
                Debug.Log("It's Case 5 v-e-e or 13 e-e-v");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_3(poly, AinB);
                break;
            //4 (7)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Vertex):
                Debug.Log("It's Case 7 v-f-v");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_4(poly, AinB);
                break;
            //5 (8) and Sym-5 (16)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Edge):
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Vertex):
                Debug.Log("It's Case 8 v-f-e or 16 e-f-v");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_5(poly, AinB);
                break;
            //6 (9) and Sym-6 (25)
            case (IntersectionSegment.PointType.Vertex, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face):
            case (IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Vertex):
                Debug.Log("It's Case 9 v-f-f or 25 f-f-v");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_6(poly, AinB);
                break;
            //7 (14)
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Edge):
                Debug.Log("It's Case 14 e-e-e");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_7(poly, AinB);
                break;
            //8 (17)
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Edge):
                Debug.Log("It's Case 17 e-f-e");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_8(poly, AinB);
                break;
            //9 (18) and Sym-9 (26)
            case (IntersectionSegment.PointType.Edge, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face):
            case (IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Edge):
                Debug.Log("It's Case 18 e-f-f or 26 f-f-e");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_9(poly, AinB);
                break;
            //10 (27)
            case (IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face, IntersectionSegment.PointType.Face):
                Debug.Log("It's case 27 f-f-f");
                yield return new WaitForSeconds(AlgoParams.DebugStepTime);
                yield return CallCase_10(poly, AinB);
                break;
            //others (2, 3, 6, 10, 11, 12, 15, 19, 20, 21, 22, 23, 24)
            default:
                throw new System.Exception("Invalid Polygon");
        }
        yield return UpdateMesh(poly);
    }
    #region Intersection Cases
    /// <summary>
    /// Vertex-Vertex-Vertex Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_1(Polygon poly, IntersectionSegment seg)
    {
        poly.Vertices[seg.PrecedingVertices.first].Status = Vertex.VertexStatus.Boundary;
        yield return RecolorPoint(seg.FirstPoint, Color.red);
    }
    /// <summary>
    /// Vertex-Edge-Vertex Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_2(Polygon poly, IntersectionSegment seg)
    {
        poly.Vertices[seg.PrecedingVertices.first].Status = Vertex.VertexStatus.Boundary;
        poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
    }
    /// <summary>
    /// Vertex-Edge-Edge or Edge-Edge-Vertex Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_3(Polygon poly, IntersectionSegment seg)
    {
        Vertex newV;
        var calculatedPoint = seg.SegmentProperties.s == IntersectionSegment.PointType.Edge ?
            seg.LineEquation.p + seg.FirstK * seg.LineEquation.d :
            seg.LineEquation.p + seg.SecondK * seg.LineEquation.d;
        var opposite = seg.SegmentProperties.s == IntersectionSegment.PointType.Edge ? seg.PrecedingVertices.last :
            seg.PrecedingVertices.first;
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
        poly.Vertices[opposite].Status = Vertex.VertexStatus.Boundary;
        if ((calculatedPoint - poly.ToGlobal(poly.Vertices[seg.PrecedingVertices.first])).sqrMagnitude <= AlgoParams.MinDist * AlgoParams.MinDist)
        {
            poly.Vertices[seg.PrecedingVertices.first].Status = Vertex.VertexStatus.Boundary;
            yield break;
        }
        if((calculatedPoint - poly.ToGlobal(poly.Vertices[seg.PrecedingVertices.last])).sqrMagnitude <= AlgoParams.MinDist * AlgoParams.MinDist)
        {
            poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
            yield break;
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
        yield return DrawPolygon(newPoly, new Color(255f, 50f, 50f));
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
        yield return DestroyPolygon(newPoly);
    }
    /// <summary>
    /// Vertex-Face-Vertex Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_4(Polygon poly, IntersectionSegment seg)
    {
        Polygon newP;
        List<Vertex> newPvertices = new List<Vertex>();
        List<Vertex> oldPvertices = new List<Vertex>();
        DivideBySegmentVertices(poly, seg, out newPvertices, out oldPvertices);
        newP = new Polygon(poly.Parent, newPvertices[0], newPvertices[1], newPvertices[2], newPvertices.Skip(3).ToArray());
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
        yield return DrawPolygon(newP, new Color(255f, 50f, 50f));
        poly.Vertices = oldPvertices.ToRing();
        poly.Parent.AddPolygon(newP);
        poly.Vertices[seg.PrecedingVertices.first].Status = 
        poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
        yield return DestroyPolygon(newP);
    }
    /// <summary>
    /// Vertex-Face-Edge or Edge-Face-Vertex Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_5(Polygon poly, IntersectionSegment seg)
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
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
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
        yield return DrawPolygon(newP, new Color(255f, 50f, 50f));
        poly.Vertices = oldPvertices.ToRing();
        poly.Parent.AddPolygon(newP);
        poly.Vertices[seg.PrecedingVertices.first].Status =
        poly.Vertices[seg.PrecedingVertices.last].Status = Vertex.VertexStatus.Boundary;
        yield return DestroyPolygon(newP);
    }
    /// <summary>
    /// Vertex-Face-Face or Face-Face-Vertex Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_6(Polygon poly, IntersectionSegment seg)
    {
        if (Mathf.Abs(seg.FirstK - seg.SecondK) < AlgoParams.MinDist)
        {
            if(seg.SegmentProperties.s == IntersectionSegment.PointType.Vertex)
            {
                seg.PrecedingVertices.last = seg.PrecedingVertices.first;
                seg.SegmentProperties.l = seg.SegmentProperties.m = seg.SegmentProperties.s;
            }
            else
            {
                seg.PrecedingVertices.first = seg.PrecedingVertices.last;
                seg.SegmentProperties.s = seg.SegmentProperties.m = seg.SegmentProperties.l;
            }
            yield return CallCase_1(poly, seg);
            yield break;
        }
        int countedPreceeding;
        Vector3 countedPoint;
        bool isFirst;
        
        if(seg.SegmentProperties.s == IntersectionSegment.PointType.Face)
        {
            countedPreceeding = seg.PrecedingVertices.first;
            countedPoint = seg.FirstPoint;
            isFirst = true;
        }
        else
        {
            countedPreceeding = seg.PrecedingVertices.last;
            countedPoint = seg.SecondPoint;
            isFirst = false;
        }
        var cosine = Mathf.Abs(Vector3.Dot(seg.LineEquation.d, 
            poly.ToGlobal(countedPoint) - 
            poly.ToGlobal(isFirst ? 
                poly.Vertices[seg.PrecedingVertices.last] : 
                poly.Vertices[seg.PrecedingVertices.first])));
        Vertex newV;
        poly.Parent.CreateVertex(countedPoint, true, out newV, out var actId);
        poly.Parent.AddDullVertices(newV);
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
        Polygon[] newPs;
        if(cosine > 0.98f)
        {
            //Is on Line
            //There are 4 polygons, so we create 3 new
            newPs = new Polygon[3];

            //Two of them are exactly the same, no matter where face vector is in segment
            newPs[0] = new Polygon(poly.Parent, newV, poly.Vertices[countedPreceeding - 1], poly.Vertices[countedPreceeding]);
            newPs[1] = new Polygon(poly.Parent, newV, poly.Vertices[countedPreceeding], poly.Vertices[(countedPreceeding + 1) % poly.Vertices.Length]);
            yield return DrawPolygon(newPs[0], new Color(255f, 50f, 50f));
            yield return DrawPolygon(newPs[1], new Color(255f, 50f, 50f));
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
            yield return DrawPolygon(newPs[2], new Color(255f, 50f, 50f));
            yield return new WaitForSeconds(AlgoParams.DebugStepTime * 2);
            yield return DestroyPolygon(newPs[0]);
            yield return DestroyPolygon(newPs[1]);
            yield return DestroyPolygon(newPs[2]);
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
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    /// <summary>
    /// Edge-Edge-Edge Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_7(Polygon poly, IntersectionSegment seg)
    {
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
        if(Mathf.Abs(seg.FirstK - seg.SecondK) < AlgoParams.MinDist)
        {
            //They are the same point
            Vertex newV;
            poly.Parent.CreateVertex(seg.LineEquation.p + seg.FirstK * seg.LineEquation.d, true, out newV, out var actId);
            poly.Parent.AddDullVertices(newV);
            var newP = new Polygon(poly.Parent, newV, poly.Vertices[(seg.PrecedingVertices.last - 1 + poly.Vertices.Length) % poly.Vertices.Length],
                poly.Vertices[seg.PrecedingVertices.last]);
            yield return DrawPolygon(newP, new Color(255f, 50f, 50f));
            yield return DestroyPolygon(poly);
            poly.Vertices[seg.PrecedingVertices.last] = newV;
            yield return DrawPolygon(poly, new Color(255f, 50f, 50f));
            poly.Parent.AddPolygon(newP);
            
            newV.Status = Vertex.VertexStatus.Boundary;
            yield return DestroyPolygon(newP);
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
            yield return DrawPolygon(newPs[0], new Color(255f, 50f, 50f));
            yield return DrawPolygon(newPs[1], new Color(255f, 50f, 50f));
            foreach (var v in newVs)
                v.Status = Vertex.VertexStatus.Boundary;
            yield return DestroyPolygon(newPs[0]);
            yield return DestroyPolygon(newPs[1]);
        }
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);
    }
    /// <summary>
    /// Edge-Face-Edge Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_8(Polygon poly, IntersectionSegment seg)
    {
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
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
        yield return DrawPolygon(newP, new Color(255f, 50f, 50f));
        poly.Vertices = oldPVertices.ToRing();

        poly.Parent.AddPolygon(newP);
        foreach(var v in newVs)
            v.Status = Vertex.VertexStatus.Boundary;
        yield return DestroyPolygon(newP);
    }
    /// <summary>
    /// Edge-Face-Face or Face-Face-Edge Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_9(Polygon poly, IntersectionSegment seg)
    {
        if (Mathf.Abs(seg.FirstK - seg.SecondK) < AlgoParams.MinDist)
        {
            if(seg.SegmentProperties.s == IntersectionSegment.PointType.Edge)
            {
                seg.SegmentProperties.m = seg.SegmentProperties.l = IntersectionSegment.PointType.Edge;
                seg.PrecedingVertices.last = seg.PrecedingVertices.first;
            }
            else
            {
                seg.SegmentProperties.m = seg.SegmentProperties.s = IntersectionSegment.PointType.Edge;
                seg.PrecedingVertices.first = seg.PrecedingVertices.last;
            }
            yield return CallCase_7(poly, seg);
            yield break;
        }
        var newVs = new Vertex[2];
        poly.Parent.CreateVertex(seg.FirstPoint, true, out newVs[0], out var actId);
        poly.Parent.AddDullVertices(newVs[0]);
        poly.Parent.CreateVertex(seg.SecondPoint, true, out newVs[1], out var ActId);
        poly.Parent.AddDullVertices(newVs[1]);
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
        var verts = poly.Vertices.ToList();
        Polygon[] newPs = new Polygon[3];
        if (seg.SegmentProperties.s == IntersectionSegment.PointType.Edge)
        {
            verts.Insert(seg.PrecedingVertices.first, newVs[0]);
            poly.Vertices = verts.ToRing();
            seg.PrecedingVertices.first++;
            seg.PrecedingVertices.last++;
            var v1 = poly.Vertices[seg.PrecedingVertices.last];
            var id2 = 1;
            var id3 = seg.PrecedingVertices.last - 1;
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
        yield return DrawPolygon(newPs[0], new Color(255f, 50f, 50f));
        yield return WaitForKeyToPush();
        yield return DrawPolygon(newPs[1], new Color(255f, 50f, 50f));
        yield return WaitForKeyToPush();
        yield return DrawPolygon(newPs[2], new Color(255f, 50f, 50f));
        yield return WaitForKeyToPush();
        newVs[0].Status = Vertex.VertexStatus.Boundary;
        newVs[1].Status = Vertex.VertexStatus.Boundary;
        yield return DestroyPolygon(newPs[0]);
        yield return DestroyPolygon(newPs[1]);
        yield return DestroyPolygon(newPs[2]);
    }
    /// <summary>
    /// Face-Face-Face Case
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="seg"></param>
    /// <returns></returns>
    private static IEnumerator CallCase_10(Polygon poly, IntersectionSegment seg)
    {
        var isCollinear =
            (first: Mathf.Abs(Vector3.Dot(seg.LineEquation.d, poly.Parent.ToGlobal(poly.Vertices[seg.PrecedingVertices.first]))) > 0.99999f,
             second: Mathf.Abs(Vector3.Dot(seg.LineEquation.d, poly.Parent.ToGlobal(poly.Vertices[seg.PrecedingVertices.last]))) > 0.99999f);
        yield return RecolorPoint(seg.FirstPoint, Color.red);
        yield return RecolorPoint(seg.SecondPoint, Color.red);
        bool isOnePoint = Mathf.Abs(seg.FirstK - seg.SecondK) < AlgoParams.MinDist;
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
        foreach(var p in newPs)
            yield return DrawPolygon(p, new Color(255f, 50f, 50f));

        poly.Parent.AddPolygon(newPs[0]);
        for(int i = 1; i < newPs.Length; i++)
            poly.Parent.AddPolygon(newPs[i]);
        for (int i = 0; i < newVs.Length; i++)
            newVs[i].Status = Vertex.VertexStatus.Boundary;
        yield return new WaitForSeconds(AlgoParams.DebugStepTime);

        foreach (var p in newPs)
            yield return DestroyPolygon(p);
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
    public static IEnumerator UpdateMesh(Polygon poly)
    {
        foreach(var p in poly.Parent.Polygons)
        {
            foreach(var v in p.Vertices)
            {
                yield return DrawPoint(p.ToGlobal(v), "UpdateMesh's");
            }
        }
        poly.Parent.UpdateMesh();
    }
}
