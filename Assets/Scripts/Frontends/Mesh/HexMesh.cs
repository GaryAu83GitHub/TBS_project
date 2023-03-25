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

        if (aDir <= HexDirection.SE)
        {
            TriangulateConnection(aDir, aCell, v1, v2);
        }
    }

    private void TriangulateConnection(HexDirection aDir, HexCell aCell, Vector3 v1, Vector3 v2)
    {
        HexCell neighbor = aCell.GetNeighbor(aDir);
        if(neighbor == null)
            return;

        Vector3 bridge = HexMetrics.GetBridge(aDir);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;
        v3.y = v4.y = neighbor.Elevation * HexMetrics.ElevationStep;

        if(aCell.GetEdgeType(aDir) == HexEdgeType.SLOPE)
        {
            TriangulateEdgeTerraces(v1, v2, aCell, v3, v4, neighbor);
        }
        else
        {
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(aCell.Color, neighbor.Color);
        }

        HexCell nextNeighbor = aCell.GetNeighbor(aDir.Next());
        if(aDir <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = v2 + HexMetrics.GetBridge(aDir.Next());
            v5.y = nextNeighbor.Elevation * HexMetrics.ElevationStep;

            if(aCell.Elevation <= neighbor.Elevation)
            {
                if(aCell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(v2, aCell, v4, neighbor, v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor, v2, aCell, v4, neighbor);
                }
            }
            else if(neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, aCell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, v2, aCell, v4, neighbor);
            }
            //AddTriangle(v2, v4, v5);
            //AddTriangleColor(aCell.Color, neighbor.Color, nextNeighbor.Color);
        }
    }

    private void TriangulateEdgeTerraces(Vector3 beginLeft, Vector3 beginRight, HexCell beginCell, Vector3 endLeft, Vector3 endRight, HexCell endCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        AddQuad(beginLeft, beginRight, v3, v4);
        AddQuadColor(beginCell.Color, c2);

        for(int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c2;
            v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
            v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);

            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2);
        }

        AddQuad(v3, v4, endLeft, endRight);
        AddQuadColor(c2, endCell.Color);
    }

    private void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 rigth, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if(leftEdgeType == HexEdgeType.SLOPE)
        {
            if(rightEdgeType == HexEdgeType.SLOPE)
            {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, rigth, rightCell);
                return;
            }
            if(rightEdgeType == HexEdgeType.FLAT)
            {
                TriangulateCornerTerraces(left, leftCell, rigth, rightCell, bottom, bottomCell);
                return;
            }
        }
        if(rightEdgeType == HexEdgeType.SLOPE)
        {
            if(leftEdgeType == HexEdgeType.FLAT)
            {
                TriangulateCornerTerraces(rigth, rightCell, bottom, bottomCell, left, leftCell);
                return;
            }
        }

        AddTriangle(bottom, left, rigth);
        AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
    }

    private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 rigth, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, rigth, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;

            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, rigth, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);

            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }

        AddQuad(v3, v4, left, rigth);
        AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
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
