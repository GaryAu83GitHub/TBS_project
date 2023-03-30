using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    private Mesh myHexMesh;

    private MeshCollider myCollider;

    public static List<Vector3> myVertices = new();
    public static List<Color> myColors = new();
    public static List<int> myTriangles = new();

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = myHexMesh = new Mesh();
        myCollider = gameObject.AddComponent<MeshCollider>();

        myHexMesh.name = "Hex Mesh";
    }

    public void Triangulate(HexCell[] someCells)
    {
        myHexMesh.Clear();
        myVertices.Clear();
        myColors.Clear();
        myTriangles.Clear();

        for (int i = 0; i < someCells.Length; i++)
            Triangulate(someCells[i]);

        myHexMesh.vertices = myVertices.ToArray();
        myHexMesh.colors = myColors.ToArray();
        myHexMesh.triangles = myTriangles.ToArray();
        myHexMesh.RecalculateNormals();

        myCollider.sharedMesh = myHexMesh;
    }

    private void Triangulate(HexCell aCell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, aCell);
        }
    }

    private void Triangulate(HexDirection aDir, HexCell aCell)
    {
        Vector3 center = aCell.Position;
        EdgeVertices e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(aDir),
            center + HexMetrics.GetSecondSolidCorner(aDir)
            );

        if (aCell.HasRiverThroughEdge(aDir))
            e.v3.y = aCell.StreamBedY;

        TriangulateEdgeFan(center, e, aCell.Color);

        if (aDir <= HexDirection.SE)
        {
            TriangulateConnection(aDir, aCell, e);
        }
    }

    private void TriangulateConnection(HexDirection aDir, HexCell aCell, EdgeVertices e1)
    {
        HexCell neighbor = aCell.GetNeighbor(aDir);
        if (neighbor == null)
            return;

        Vector3 bridge = HexMetrics.GetBridge(aDir);
        bridge.y = neighbor.Position.y - aCell.Position.y;
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge
            );

        if (aCell.HasRiverThroughEdge(aDir))
            e2.v3.y = neighbor.StreamBedY;

        if (aCell.GetEdgeType(aDir) == HexEdgeType.SLOPE)
        {
            TriangulateEdgeTerraces(e1, aCell, e2, neighbor);
        }
        else
        {
            TriangulateEdgeStrip(e1, aCell.Color, e2, neighbor.Color);
        }

        HexCell nextNeighbor = aCell.GetNeighbor(aDir.Next());
        if (aDir <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(aDir.Next());
            v5.y = nextNeighbor.Position.y;

            if (aCell.Elevation <= neighbor.Elevation)
            {
                if (aCell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(e1.v5, aCell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor, e1.v5, aCell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, aCell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, e1.v5, aCell, e2.v5, neighbor);
            }
        }
    }

    private void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;

            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);

            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.Color);
    }

    private void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 rigth, HexCell rightCell)
    {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.SLOPE)
        {
            if (rightEdgeType == HexEdgeType.SLOPE)
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, rigth, rightCell);
            else if (rightEdgeType == HexEdgeType.FLAT)
                TriangulateCornerTerraces(left, leftCell, rigth, rightCell, bottom, bottomCell);
            else
            {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, rigth, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.SLOPE)
        {
            if (leftEdgeType == HexEdgeType.FLAT)
                TriangulateCornerTerraces(rigth, rightCell, bottom, bottomCell, left, leftCell);
            else
            {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, rigth, rightCell);
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.SLOPE)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(rigth, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCell, rigth, rightCell, bottom, bottomCell);
            }
        }
        else
        {
            AddTrianglePerturb(bottom, left, rigth);
            AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 rigth, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, rigth, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        AddTrianglePerturb(begin, v3, v4);
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

    private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0f)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        TriangulateBoundaryTriangleUnperturbed(begin, beginCell, left, leftCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.SLOPE)
        {
            TriangulateBoundaryTriangleUnperturbed(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

        TriangulateBoundaryTriangleUnperturbed(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.SLOPE)
        {
            TriangulateBoundaryTriangleUnperturbed(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell, 
        Vector3 left, HexCell leftCell, 
        Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        AddTrianglePerturb(begin, v2, boundary);
        AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;

            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);

            AddTrianglePerturb(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);

        }

        AddTrianglePerturb(v2, left, boundary);
        AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    private void TriangulateBoundaryTriangleUnperturbed(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        AddTriangleUnperturbed(Perturb(begin), v2, boundary);
        AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;

            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);

            AddTriangleUnperturbed(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);

        }

        AddTriangleUnperturbed(v2, Perturb(left), boundary);
        AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    private Vector3 Perturb(Vector3 aPosition)
    {
        Vector4 sample = HexMetrics.SampleNoise(aPosition);

        aPosition.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
        //aPosition.y += (sample.y * 2f - 1f) * HexMetrics.CellPerturbStrength;
        aPosition.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;

        return aPosition;
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        AddTrianglePerturb(center, edge.v1, edge.v2);
        AddTriangleColor(color);
        
        AddTrianglePerturb(center, edge.v2, edge.v3);
        AddTriangleColor(color);

        AddTrianglePerturb(center, edge.v3, edge.v4);
        AddTriangleColor(color);

        AddTrianglePerturb(center, edge.v4, edge.v5);
        AddTriangleColor(color);
    }

    private void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
    {
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        AddQuadColor(c1, c2);

        AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        AddQuadColor(c1, c2);

        AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        AddQuadColor(c1, c2);

        AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        AddQuadColor(c1, c2);
    }

    private void AddTrianglePerturb(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = myVertices.Count;

        myVertices.Add(Perturb(v1));
        myVertices.Add(Perturb(v2));
        myVertices.Add(Perturb(v3));

        myTriangles.Add(vertexIndex);
        myTriangles.Add(vertexIndex + 1);
        myTriangles.Add(vertexIndex + 2);
    }

    private void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
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

        myVertices.Add(Perturb(v1));
        myVertices.Add(Perturb(v2));
        myVertices.Add(Perturb(v3));
        myVertices.Add(Perturb(v4));

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