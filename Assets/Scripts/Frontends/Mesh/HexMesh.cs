using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    private Mesh myHexMesh;
    private List<Vector3> myVertices;
    private List<int> myTriangles;

    private MeshCollider myCollider;

    private List<Color> myColors;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = myHexMesh = new Mesh();
        myCollider = gameObject.AddComponent<MeshCollider>();

        myHexMesh.name = "Hex Mesh";
        myVertices = new List<Vector3>();
        myColors = new List<Color>();
        myTriangles = new List<int>();
    }

    public void Triangulate(HexCell[] someCells)
    {
        myHexMesh.Clear();
        myVertices.Clear();
        myColors.Clear();
        myTriangles.Clear();

        for(int i = 0; i < someCells.Length; i++)
            Triangulate(someCells[i]);

        myHexMesh.vertices = myVertices.ToArray();
        myHexMesh.colors = myColors.ToArray();
        myHexMesh.triangles = myTriangles.ToArray();
        myHexMesh.RecalculateNormals();

        myCollider.sharedMesh = myHexMesh;
    }

    private void Triangulate(HexCell aCell)
    {
        for(HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, aCell);
        }
    }

    private void Triangulate(HexDirection aDir, HexCell aCell)
    {
        Vector3 center = aCell.transform.localPosition;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(aDir);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(aDir);
        
        AddTriangle(center, v1, v2);
        AddTriangleColor(aCell.Color);

        Vector3 bridge = HexMetrics.GetBridge(aDir);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;

        AddQuad(v1, v2, v3, v4);

        HexCell prevNeighbor = aCell.GetNeighbor(aDir.Previous()) ?? aCell;
        HexCell neighbor = aCell.GetNeighbor(aDir) ?? aCell;
        HexCell nextNeighbor = aCell.GetNeighbor(aDir.Next()) ?? aCell;

        Color bridgeColor = (aCell.Color + neighbor.Color) * .5f;
        AddQuadColor(aCell.Color, bridgeColor);

        AddTriangle(v1, center + HexMetrics.GetFirstCorner(aDir), v3);
        AddTriangleColor(
            aCell.Color,
            (aCell.Color + prevNeighbor.Color + neighbor.Color) / 3f,
            bridgeColor
            );

        AddTriangle(v2, v4, center + HexMetrics.GetSecondCorner(aDir));
        AddTriangleColor(
            aCell.Color,
            bridgeColor,
            (aCell.Color + neighbor.Color + nextNeighbor.Color) / 3f
            );
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = myVertices.Count;

        myVertices.Add(v1);
        myVertices.Add(v2);
        myVertices.Add(v3);
        
        myTriangles.Add(vertexIndex);
        myTriangles.Add(vertexIndex + 1);
        myTriangles.Add(vertexIndex + 2);
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = myVertices.Count;

        myVertices.Add(v1);
        myVertices.Add(v2);
        myVertices.Add(v3);
        myVertices.Add(v4);

        myTriangles.Add(vertexIndex);
        myTriangles.Add(vertexIndex + 2);
        myTriangles.Add(vertexIndex + 1);
        myTriangles.Add(vertexIndex + 1);
        myTriangles.Add(vertexIndex + 2);
        myTriangles.Add(vertexIndex + 3);
    }

    private void AddTriangleColor(Color aColor)
    {
        myColors.Add(aColor);
        myColors.Add(aColor);
        myColors.Add(aColor);
    }

    private void AddTriangleColor(Color aColor1, Color aColor2, Color aColor3)
    {
        myColors.Add(aColor1);
        myColors.Add(aColor2);
        myColors.Add(aColor3);
    }

    private void AddQuadColor(Color aColor1, Color aColor2)
    {
        myColors.Add(aColor1);
        myColors.Add(aColor1);
        myColors.Add(aColor2);
        myColors.Add(aColor2);
    }

    private void AddQuadColor(Color aColor1, Color aColor2, Color aColor3, Color aColor4)
    {
        myColors.Add(aColor1);
        myColors.Add(aColor2);
        myColors.Add(aColor3);
        myColors.Add(aColor4);
    }
}
