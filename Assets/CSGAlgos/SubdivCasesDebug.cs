using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubdivCasesDebug : MonoBehaviour
{
    [SerializeField]
    private PrimitiveMesh Instrument;
    [SerializeField]
    private PrimitiveMesh SculptMesh;
    [SerializeField]
    private Transform debugInstrumentTarget;
    private Vector3 defaultScale;
    // Start is called before the first frame update
    private void Start()
    {
        defaultScale = debugInstrumentTarget.localScale;
    }
    public void Update()
    {
        var k = new[]
        {
            Input.GetKeyDown(KeyCode.Alpha1), //0
            Input.GetKeyDown(KeyCode.Alpha2), //1
            Input.GetKeyDown(KeyCode.Alpha3), //2
            Input.GetKeyDown(KeyCode.Alpha4), //3
            Input.GetKeyDown(KeyCode.Alpha5), //4
            Input.GetKeyDown(KeyCode.Alpha6), //5
            Input.GetKeyDown(KeyCode.Alpha7), //6
            Input.GetKeyDown(KeyCode.Alpha8), //7
            Input.GetKeyDown(KeyCode.Alpha9), //8
            Input.GetKeyDown(KeyCode.Alpha0)  //9
        };
        //Face Face Face
        if (k[9])
        {
            debugInstrumentTarget.position = new Vector3(1f, 1.6f, 0f);
            debugInstrumentTarget.rotation = Quaternion.Euler(0f, 60f, 0f);
            debugInstrumentTarget.localScale = defaultScale;
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
        //Edge Face Face
        else if (k[8])
        {
            debugInstrumentTarget.position = new Vector3(0f, 1.3f, 1.4f);
            debugInstrumentTarget.rotation = Quaternion.identity;
            debugInstrumentTarget.localScale = defaultScale;
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
        //Edge Face Edge
        else if (k[7])
        {
            debugInstrumentTarget.position = new Vector3(0.348f, 3.32f, -0.232f);
            debugInstrumentTarget.rotation = Quaternion.Euler(0f, 57.8f, 0f);
            debugInstrumentTarget.localScale = defaultScale;
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
        //Edge Edge Edge
        else if (k[6])
        {
            debugInstrumentTarget.position = new Vector3(1.104f, 0.4026f, 0.839f);
            debugInstrumentTarget.rotation = Quaternion.Euler(0f, 57.78f, -0.12f);
            debugInstrumentTarget.localScale = defaultScale;
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
        //Vertex Face Face
        else if (k[5])
        {
            debugInstrumentTarget.position = new Vector3(2.369f, 0.379f, -1.708f);
            debugInstrumentTarget.rotation = Quaternion.Euler(3.724f, 9.184f, -21.889f);
            debugInstrumentTarget.localScale = defaultScale;
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
        //Vertex Face Edge
        else if (k[4])
        {
            debugInstrumentTarget.position = new Vector3(1.796f, 3.048f, 1.423f);
            debugInstrumentTarget.rotation = Quaternion.Euler(-8.199f, 72.134f, 29.165f);
            debugInstrumentTarget.localScale = new Vector3(10f, 10f, 10f);
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
        //Vertex Face Vertex
        else if (k[3])
        {
            //Impossible in Unity
        }
        //Vertex Edge Edge
        else if (k[2])
        {
            debugInstrumentTarget.position = new Vector3(2.5454f, 0.4212f, -1.4842f);
            debugInstrumentTarget.rotation = Quaternion.Euler(0f, 57.78f, -0.1f);
            debugInstrumentTarget.localScale = defaultScale;
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
        //Vertex Edge Vertex
        else if (k[1])
        {
            debugInstrumentTarget.position = new Vector3(3.26f, 2.155f, 1.35f);
            debugInstrumentTarget.rotation = Quaternion.Euler(0f, 57.78f, -0.15f);
            debugInstrumentTarget.localScale = new Vector3(12f, 12f, 12f);
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
        //Vertex Vertex Vertex
        else if(k[0])
        {
            debugInstrumentTarget.position = new Vector3(2.935f, 0.43f, -1.707f);
            debugInstrumentTarget.rotation = Quaternion.identity;
            debugInstrumentTarget.localScale = defaultScale;
            DebugAnimator.Extract(SculptMesh, Instrument);
        }
    }
}
