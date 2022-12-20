using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class DebugAnimator : MonoBehaviour
{
    [SerializeField]
    private Dictionary<object, GameObject> DrawedObjects;
    private void Awake()
    {
        DrawedObjects = new Dictionary<object, GameObject>();
    }
    // Start is called before the first frame update
    public static DebugAnimator GlobalAnimator
    {
        get => Camera.main.GetComponent<DebugAnimator>();
    }
    public static GameObject DrawPolygon(Polygon poly)
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
        return polygon;
    }

    public static GameObject DrawLine((Vector3 p, Vector3 d) lineEquation)
    {
        if (GlobalAnimator.DrawedObjects.ContainsKey(lineEquation))
            return GlobalAnimator.DrawedObjects[lineEquation];
        var line = new GameObject();
        line.name = "IntersectionLine";
        var renderer = line.AddComponent<LineRenderer>();
        renderer.widthMultiplier = 0.04f;
        renderer.material = new Material(Shader.Find("Specular"));
        renderer.material.color = Color.blue;
        renderer.positionCount = 3;
        renderer.SetPosition(0, lineEquation.p - 1000 * lineEquation.d);
        renderer.SetPosition(1, lineEquation.p);
        renderer.SetPosition(2, lineEquation.p + 1000 * lineEquation.d);
        GlobalAnimator.DrawedObjects.Add(lineEquation, line);
        Camera.main.Render();
        return line;
    }
    public static void DestroyPolygon(Polygon poly)
    {
        GameObject.DestroyImmediate(GlobalAnimator.DrawedObjects[poly]);
    }
    public static void DestroyLine((Vector3 p, Vector3 d) lineEquation)
    {
        GameObject.DestroyImmediate(GlobalAnimator.DrawedObjects[lineEquation]);
    }
}
