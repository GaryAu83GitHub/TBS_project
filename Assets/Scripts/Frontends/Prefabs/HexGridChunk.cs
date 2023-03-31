using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;

public class HexGridChunk : MonoBehaviour
{
    public HexMesh terrain, river;
    private HexCell[] myCells;
    //private HexMesh myHexMesh;
    Canvas myGridCanvas;

    private void Awake()
    {
        myGridCanvas = GetComponentInChildren<Canvas>();

        myCells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        ShowUI(false);
    }

    private void LateUpdate()
    {
        Triangulate();
        enabled = false;
    }

    public void AddCell(int index, HexCell aCell)
    {
        myCells[index] = aCell;
        aCell.Chunk = this;
        aCell.transform.SetParent(transform, false);
        aCell.UIRect.SetParent(myGridCanvas.transform, false);
    }

    public void Refresh()
    {
        //myHexMesh.Triangulate(myCells);
        enabled = true;
    }

    public void ShowUI(bool visible)
    {
        myGridCanvas.gameObject.SetActive(visible);
    }

    public void Triangulate()
    {
        terrain.Clear();
        river.Clear();

        for (int i = 0; i < myCells.Length; i++)
            Triangulate(myCells[i]);

        terrain.Apply();
        river.Apply();
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

        if (aCell.HasRiver)
        {
            if (aCell.HasRiverThroughEdge(aDir))
            {
                e.v3.y = aCell.StreamBedY;
                if (aCell.HasRiverBeginOrEnd)
                    TriangulateWithRiverBeginOrEnd(aDir, aCell, center, e);
                else
                    TriangulateWithRiver(aDir, aCell, center, e);
            }
            else
            {
                TriangulateAdjacentToRiver(aDir, aCell, center, e);
            }
        }
        else
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
            terrain.AddTriangle(bottom, left, rigth);
            terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 rigth, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, rigth, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(beginCell.Color, c3, c4);

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

            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }

        terrain.AddQuad(v3, v4, left, rigth);
        terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0f)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        TriangulateBoundaryTriangleUnperturbed(begin, beginCell, left, leftCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.SLOPE)
        {
            TriangulateBoundaryTriangleUnperturbed(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

        TriangulateBoundaryTriangleUnperturbed(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.SLOPE)
        {
            TriangulateBoundaryTriangleUnperturbed(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        terrain.AddTriangle(begin, v2, boundary);
        terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;

            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);

            terrain.AddTriangle(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);

        }

        terrain.AddTriangle(v2, left, boundary);
        terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    private void TriangulateBoundaryTriangleUnperturbed(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor)
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;

            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);

            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);

        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }



    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangleColor(color);

        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangleColor(color);

        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangleColor(color);

        terrain.AddTriangle(center, edge.v4, edge.v5);
        terrain.AddTriangleColor(color);
    }

    private void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuadColor(c1, c2);

        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuadColor(c1, c2);

        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuadColor(c1, c2);

        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        terrain.AddQuadColor(c1, c2);
    }

    private void TriangulateWithRiver(HexDirection aDir, HexCell aCell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;

        if (aCell.HasRiverThroughEdge(aDir.Opposite()))
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(aDir.Previous()) * .25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(aDir.Next()) * .25f;
        }
        else if (aCell.HasRiverThroughEdge(aDir.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (aCell.HasRiverThroughEdge(aDir.Previous()))
        {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        else if (aCell.HasRiverThroughEdge(aDir.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(aDir.Next()) * (.5f * HexMetrics.InnerToOuter);

        }
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(aDir.Previous()) * (.5f * HexMetrics.InnerToOuter);
            centerR = center;
        }
        center = Vector3.Lerp(centerL, centerR, .5f);

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, .5f),
            Vector3.Lerp(centerR, e.v5, .5f),
            1f / 6f
            );

        m.v3.y = center.y = e.v3.y;

        TriangulateEdgeStrip(m, aCell.Color, e, aCell.Color);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddTriangleColor(aCell.Color);

        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuadColor(aCell.Color);

        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddQuadColor(aCell.Color);

        terrain.AddTriangle(centerR, m.v4, m.v5);
        terrain.AddTriangleColor(aCell.Color);

        TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, aCell.RiverSurfaceY);
        TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, aCell.RiverSurfaceY);
    }

    private void TriangulateWithRiverBeginOrEnd(HexDirection aDir, HexCell aCell, Vector3 center, EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, .5f),
            Vector3.Lerp(center, e.v5, .5f)
            );

        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, aCell.Color, e, aCell.Color);
        TriangulateEdgeFan(center, m, aCell.Color);
    }

    private void TriangulateAdjacentToRiver(HexDirection aDir, HexCell aCell, Vector3 center, EdgeVertices e)
    {
        if (aCell.HasRiverThroughEdge(aDir.Next()))
        {
            if (aCell.HasRiverThroughEdge(aDir.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(aDir) * (HexMetrics.InnerToOuter * .5f);
            }
            else if (aCell.HasRiverThroughEdge(aDir.Previous2()))
            {
                center += HexMetrics.GetFirstSolidCorner(aDir) * .25f;
            }
        }
        else if (aCell.HasRiverThroughEdge(aDir.Previous()) && aCell.HasRiverThroughEdge(aDir.Next2()))
        {
            center += HexMetrics.GetSecondSolidCorner(aDir) * .25f;
        }

        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, .5f),
            Vector3.Lerp(center, e.v5, .5f)
            );

        TriangulateEdgeStrip(m, aCell.Color, e, aCell.Color);
        TriangulateEdgeFan(center, m, aCell.Color);
    }

    private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y)
    {
        v1.y = v2.y = v3.y = v4.y = y;
        river.AddQuad(v1, v2, v3, v4);
        river.AddQuadUV(0f, 1f, 0f, 1f);
    }
}
