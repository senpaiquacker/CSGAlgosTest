using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class Vertex
{
    public enum VertexStatus
    {
        Unknown = 0,
        Inside = 1,
        Outside = 5,
        Boundary = 10
    }
    public Vector3 LocalPosition { get; set; }
    public int IdInMesh { get; private set; }
    public Vector3 GetGlobal(Transform parent) => parent.localToWorldMatrix.MultiplyPoint3x4(LocalPosition);

    public List<Vertex> neighbours;

    public VertexStatus Status { get; set; }

    public List<Vertex> GetAreaWithSameStatus()
    {
        var answ = new List<Vertex>();
        foreach(var v in neighbours)
        {
            if(v.Status == Status)
                answ.AddRange(v.GetAreaWithSameStatus());
        }
        answ.Add(this);
        return answ.Distinct().ToList();
    }
    private Vertex()
    {
        neighbours = new List<Vertex>();
        Status = VertexStatus.Unknown;
    }
    private Vertex(Vector3 position)
    {
        neighbours = new List<Vertex>();
        LocalPosition = position;
    }
    public static Vertex CreateVertex(PrimitiveMesh mesh, Vector3 localPosition)
    {
        var k = new Vertex(localPosition);
        k.IdInMesh = mesh.Vertices.Length;
        return k;
    }
    public void MarkVerticeWithUnknown(VertexStatus status)
    {
        this.Status = status;
        foreach(var nb in neighbours)
            if(nb.Status == VertexStatus.Unknown)
                nb.MarkVerticeWithUnknown(status);
    }
    public override string ToString()
    {
        return LocalPosition.ToString("F4");
    }
}
