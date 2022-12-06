using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(PrimitiveMesh))]
public class InstrumentHandler : MonoBehaviour
{
    private Collider sensor;
    private void Awake()
    {
        sensor = GetComponent<Collider>();
        sensor.isTrigger = true;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var x = Input.GetAxis("Horizontal");
        var y = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(x, 0, y).normalized * Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<SculptMesh>() != null)
        {
            GetComponent<MeshRenderer>().materials[1].color = Color.red;
            OperationsAlgorithms.Extract(other.GetComponent<PrimitiveMesh>(), GetComponent<PrimitiveMesh>());
        }
    }
    private void OnTriggerStay(Collider other)
    {
        OnTriggerEnter(other);
    }
    private void OnTriggerExit(Collider other)
    {
        GetComponent<MeshRenderer>().materials[1].color = Color.white;
    }
}
