using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[RequireComponent(typeof(MeshFilter))]
public class PrimitiveMesh : MonoBehaviour
{
    public (Vector3 min, Vector3 max) ObjectExtent
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


    public List<Vertex> UpdatedVertices = new List<Vertex>();
    private ((int x, int y, int z) min, (int x, int y, int z) max) extentIds;

    #region Polygons And Vertices
    private Polygon[] polygons;
    public Polygon[] Polygons { get => polygons; }

    private Vertex[] vertices;
    public Vertex[] Vertices { get => vertices; }
    #endregion

    private Mesh mesh;
    [SerializeField]
    private GameObject debugSphere;
    private GameObject[] debugCopies;
    public void UpdatePolys(Polygon[] polys)
    {
        polygons = polys;
    }
    public void AddPolygon(Polygon newPoly, params Vertex[] newVerts)
    {
        if (newVerts != null)
        {
            vertices = vertices.Concat(newVerts).ToArray();
            mesh.vertices = vertices.Select(a => a.LocalPosition).ToArray();
        }
        mesh.triangles = mesh.triangles.Concat(newPoly.Vertices.Select(a => a.IdInMesh)).ToArray();
    }
    public void UpdateMesh()
    {
        vertices = vertices.Distinct().ToArray();
        mesh.vertices = polygons.SelectMany(a => a.Vertices.Select(a => a.LocalPosition)).ToArray();
        var i = 0;
        mesh.triangles = mesh.vertices.Select(a => i++).ToArray();
    }
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = new Vertex[0];
        polygons = new Polygon[mesh.triangles.Length / 3];
        Dictionary<int, int> duplicates = new Dictionary<int, int>();
        int i = 0;
        for(int k = 0; k < mesh.triangles.Length; k+=3)
        {
            var trios = new Vertex[3];
            for (int j = 0; j < 3; j++)
            {
                if (duplicates.ContainsKey(mesh.triangles[k + j]))
                    continue;
                if (CreateVertex(mesh.vertices[mesh.triangles[k + j]], false, out var newV, out var actId))
                    vertices = vertices.Concat(new[] { newV }).ToArray();
                duplicates[mesh.triangles[k + j]] = actId;
                trios[j] = vertices[actId];
            }
            polygons[k / 3] = new Polygon(this, trios[0], trios[1], trios[2]); 
        }
        extentIds = Extent.UpdateExtent(vertices.Select(a => a.LocalPosition));
        foreach (var v in vertices)
            v.neighbours = v.neighbours.Distinct().ToList();

        debugCopies = new GameObject[8];
        for (int k = 0; k < 8; k++)
            debugCopies[k] = Instantiate(debugSphere, Vector3.zero, Quaternion.identity);
    }

    public bool CreateVertex(Vector3 position, bool isGlobal, out Vertex vert, out int actId)
    {
        vert = null;
        actId = 0;
        var local = isGlobal ? ToLocal(position) : position;
        foreach(var i in vertices)
        {
            
            if ((local - i.LocalPosition).sqrMagnitude < 0.00001f)
                return false;
            actId++;
        }
        vert = Vertex.CreateVertex(this, local);
        return true;
    }
    public void AddDullVertices(params Vertex[] newVerts)
    {
        vertices = vertices.Concat(newVerts).ToArray();
    }
    // Update is called once per frame
    void Update()
    {
        DrawDebug();
    }
    public void DrawDebug()
    {
        for (int k = 0; k < 8; k++)
        {
            var debugCoord = new Vector3();
            if (k % 2 == 0)
                debugCoord.z = ToGlobal(Vertices[extentIds.min.z].LocalPosition).z;
            else
                debugCoord.z = ToGlobal(Vertices[extentIds.max.z].LocalPosition).z;
            if (k % 4 < 2)
                debugCoord.y = ToGlobal(Vertices[extentIds.min.y].LocalPosition).y;
            else
                debugCoord.y = ToGlobal(Vertices[extentIds.max.y].LocalPosition).y;
            if (k < 4)
                debugCoord.x = ToGlobal(Vertices[extentIds.min.x].LocalPosition).x;
            else
                debugCoord.x = ToGlobal(Vertices[extentIds.max.x].LocalPosition).x;
            debugCopies[k].transform.position = debugCoord;
        }
    }
    #region MatrixTransform
    public (Vector3 emin, Vector3 emax) ToGlobal() =>
    (
        emin: transform.localToWorldMatrix.MultiplyPoint3x4(ObjectExtent.min),
        emax: transform.localToWorldMatrix.MultiplyPoint3x4(ObjectExtent.max)
    );
    public (Vector3 normal, float D) ToGlobal((Vector3 normal, float D) planeEquation)
    {
        var plane = new Vector4(planeEquation.normal.x, planeEquation.normal.y, planeEquation.normal.z, planeEquation.D);
        var newPlane = transform.localToWorldMatrix.inverse.transpose * plane;
        var mag = ((Vector3)newPlane).magnitude;
        return (newPlane / mag, newPlane.w / mag);
    }
    public Vector3 ToGlobal(Vector3 point) => transform.localToWorldMatrix.MultiplyPoint3x4(point);
    public Vector3 ToGlobal(Vertex vertex) => transform.localToWorldMatrix.MultiplyPoint3x4(vertex.LocalPosition);
    public (Vector3 emin, Vector3 emax) ToGlobal(Polygon polygon) =>
    (
        emin: transform.localToWorldMatrix.MultiplyPoint3x4(polygon.PolyExtent.min),
        emax: transform.localToWorldMatrix.MultiplyPoint3x4(polygon.PolyExtent.max)
    );

    public Plane ToGlobal(Plane p) => transform.localToWorldMatrix.TransformPlane(p);
    public Plane ToGlobal(Plane p, Vector3 pointOnPlane)
    {
        var plane = transform.localToWorldMatrix.TransformPlane(p);
        plane.distance = -Vector3.Dot(pointOnPlane, plane.normal);
        return plane;
    }
    public Vector3 ToLocal(Vector3 anyPoint) => transform.worldToLocalMatrix.MultiplyPoint3x4(anyPoint);
    #endregion
}
