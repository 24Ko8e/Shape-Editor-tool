using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeCreator : MonoBehaviour
{
    public MeshFilter meshFilter;
    [HideInInspector]
    public List<Shape> shapes = new List<Shape>();

    [HideInInspector]
    public bool showShapesList;

    public float handleRadius = 0.5f;

    public void UpdateMeshDisplay()
    {
        CompositeShape compositeShape = new CompositeShape(shapes);
        meshFilter.mesh = compositeShape.GetMesh();
    }
}