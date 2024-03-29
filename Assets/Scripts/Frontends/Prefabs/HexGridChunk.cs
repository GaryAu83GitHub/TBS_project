using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;

public class HexGridChunk : MonoBehaviour
{
    public HexMesh terrain, rivers, roads, water, waterShore, estuaries;
    public HexFeatureManager features;

    private HexCell[] myCells;
    //private HexMesh myHexMesh;
    Canvas myGridCanvas;

    static Color weights1 = new Color(1f, 0f, 0f);
    static Color weights2 = new Color(0f, 1f, 0f);
    static Color weights3 = new Color(0f, 0f, 1f);

    private void Awake()
    {
        myGridCanvas = GetComponentInChildren<Canvas>();

        myCells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        //ShowUI(false);
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
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();
        features.Clear();

        for (int i = 0; i < myCells.Length; i++)
            Triangulate(myCells[i]);

        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
        features.Apply();
    }

    private void Triangulate(HexCell aCell)
    {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            Triangulate(d, aCell);
        }

        if (!aCell.IsUnderwater)
        {
            if (!aCell.HasRiver && !aCell.HasRoads)
                features.AddFeature(aCell, aCell.Position);

            if (aCell.IsSpecial)
                features.AddSpecialFeature(aCell, aCell.Position);
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
        {
            TriangulateWithoutRiver(aDir, aCell, center, e);

            if (!aCell.IsUnderwater && !aCell.HasRoadThroughEdge(aDir))
                features.AddFeature(aCell, (center + e.v1 + e.v5) * (1f / 3f));
        }

        if (aDir <= HexDirection.SE)
        {
            TriangulateConnection(aDir, aCell, e);
        }

        if(aCell.IsUnderwater)
            TriangulateWater(aDir, aCell, center);
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

        bool hasRiver = aCell.HasRiverThroughEdge(aDir);
        bool hasRoad = aCell.HasRoadThroughEdge(aDir);

        if (hasRiver)
        {
            e2.v3.y = neighbor.StreamBedY;
            Vector3 indices;
            indices.x = indices.z = aCell.Index;
            indices.y = neighbor.Index;

            if (!aCell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                {
                    TriangulateRiverQuad(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        aCell.RiverSurfaceY, neighbor.RiverSurfaceY, .8f,
                        aCell.HasIncomingRiver && aCell.IncomingRiver == aDir,
                        indices);
                }
                else if(aCell.Elevation > neighbor.WaterLevel)
                {
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        aCell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY, indices);
                }
            }
            else if (!neighbor.IsUnderwater && neighbor.Elevation > aCell.WaterLevel)
            {
                TriangulateWaterfallInWater(
                    e2.v4, e2.v2, e1.v4, e1.v2,
                    neighbor.RiverSurfaceY, aCell.RiverSurfaceY,
                    aCell.WaterSurfaceY, indices);
            }
        }

        if (aCell.GetEdgeType(aDir) == HexEdgeType.SLOPE)
        {
            TriangulateEdgeTerraces(e1, aCell, e2, neighbor, hasRoad);
        }
        else
        {
            TriangulateEdgeStrip(e1, weights1, aCell.Index, e2, weights2, neighbor.Index, hasRoad);
        }

        features.AddWall(e1, aCell, e2, neighbor, hasRiver, hasRoad);

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

    private void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad)
    {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color w2 = HexMetrics.TerraceLerp(weights1, weights2, 1);
        float i1 = beginCell.Index;
        float i2 = endCell.Index;

        TriangulateEdgeStrip(begin, weights1, i1, e2, w2, i2, hasRoad);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color w1 = w2;

            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            w2 = HexMetrics.TerraceLerp(weights1, weights2, i);

            TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, hasRoad);
        }

        TriangulateEdgeStrip(e2, w2, i1, end, weights2, i2, hasRoad);
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
            Vector3 indices;
            indices.x = bottomCell.Index;
            indices.y = leftCell.Index;
            indices.z = rightCell.Index;
            terrain.AddTriangleCellData(indices, weights1, weights2, weights3);
            //terrain.AddTriangleColor(weights1, weights2, weights3);

            //Vector3 types;
            //types.x = bottomCell.TerrainTypeIndex;
            //types.y = leftCell.TerrainTypeIndex;
            //types.z = rightCell.TerrainTypeIndex;

            //terrain.AddTriangleTerrainTypes(types);
        }

        features.AddWall(bottom, bottomCell, left, leftCell, rigth, rightCell);
    }

    private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 rigth, HexCell rightCell)
    {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, rigth, 1);
        Color w3 = HexMetrics.TerraceLerp(weights1, weights2, 1);
        Color w4 = HexMetrics.TerraceLerp(weights1, weights3, 1);
        
        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleCellData(indices, weights1, w3, w4);
        //terrain.AddTriangleColor(weights1, w3, w4);
        //terrain.AddTriangleTerrainTypes(indices);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color w1 = w3;
            Color w2 = w4;

            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, rigth, i);
            w3 = HexMetrics.TerraceLerp(weights1, weights2, i);
            w4 = HexMetrics.TerraceLerp(weights1, weights3, i);

            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadCellData(indices, w1, w2, w3, w4);
            //terrain.AddQuadColor(w1, w2, w3, w4);
            //terrain.AddQuadTerrainTypes(indices);
        }

        terrain.AddQuad(v3, v4, left, rigth);
        terrain.AddQuadCellData(indices, w3, w4, weights2, weights3);
        //terrain.AddQuadColor(w3, w4, weights2, weights3);
        //terrain.AddQuadTerrainTypes(indices);
    }

    private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0f)
        {
            b = -b;
        }

        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryWeights = Color.Lerp(weights1, weights3, b);

        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        TriangulateBoundaryTriangle(begin, weights1, left, weights2, boundary, boundaryWeights, indices);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.SLOPE)
        {
            TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
            //terrain.AddTriangleColor(weights2, weights3, boundaryWeights);
            //terrain.AddTriangleTerrainTypes(indices);
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
        Color boundaryWeights = Color.Lerp(weights1, weights2, b);

        Vector3 indices;
        indices.x = beginCell.Index;
        indices.y = leftCell.Index;
        indices.z = rightCell.Index;

        TriangulateBoundaryTriangle(right, weights3, begin, weights1, boundary, boundaryWeights, indices);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.SLOPE)
        {
            TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
        }
        else
        {
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
            //terrain.AddTriangleColor(weights2, weights3, boundaryWeights);
            //terrain.AddTriangleTerrainTypes(indices);
        }
    }

    private void TriangulateBoundaryTriangle(Vector3 begin, Color beginWeight, Vector3 left, Color leftWeight, Vector3 boundary, Color boundaryWeight, Vector3 indices)
    {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color w2 = HexMetrics.TerraceLerp(beginWeight, leftWeight, 1);

        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleCellData(indices, beginWeight, w2, boundaryWeight);
        //terrain.AddTriangleColor(beginWeight, w2, boundaryWeight);
        //terrain.AddTriangleTerrainTypes(indices);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color w1 = w2;

            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            w2 = HexMetrics.TerraceLerp(beginWeight, leftWeight, i);

            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleCellData(indices, w1, w2, boundaryWeight);
            //terrain.AddTriangleColor(w1, w2, boundaryWeight);
            //terrain.AddTriangleTerrainTypes(indices);
        }

        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleCellData(indices, w2, leftWeight, boundaryWeight);
        //terrain.AddTriangleColor(w2, leftWeight, boundaryWeight);
        //terrain.AddTriangleTerrainTypes(indices);
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
    {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangle(center, edge.v4, edge.v5);

        Vector3 indices;
        indices.x = indices.y = indices.z = index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
    }

    private void TriangulateEdgeStrip(EdgeVertices e1, Color w1, float index1, EdgeVertices e2, Color w2, float index2, bool hasRoad = false)
    {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

        Vector3 indices;
        indices.x = indices.z = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);
        terrain.AddQuadCellData(indices, w1, w2);

        if (hasRoad)
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4, w1, w2, indices);
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

        TriangulateEdgeStrip(m, weights1, aCell.Index, e, weights1, aCell.Index);

        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddTriangle(centerR, m.v4, m.v5);

        Vector3 indices;
        indices.x = indices.y = indices.z = aCell.Index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);

        if (!aCell.IsUnderwater)
        {
            bool reversed = aCell.IncomingRiver == aDir;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, aCell.RiverSurfaceY, .4f, reversed, indices);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, aCell.RiverSurfaceY, .6f, reversed, indices);
        }
    }

    private void TriangulateWithoutRiver(HexDirection aDir, HexCell aCell, Vector3 center, EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, aCell.Index);

        if(aCell.HasRoads)
        {
            Vector2 interpolators = GetRoadInterpolators(aDir, aCell);
            TriangulateRoad(
                center, 
                Vector3.Lerp(center, e.v1, interpolators.x), 
                Vector3.Lerp(center, e.v5, interpolators.y), 
                e, aCell.HasRoadThroughEdge(aDir), aCell.Index);
        }
    }

    private void TriangulateWithRiverBeginOrEnd(HexDirection aDir, HexCell aCell, Vector3 center, EdgeVertices e)
    {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, .5f),
            Vector3.Lerp(center, e.v5, .5f)
            );

        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, weights1, aCell.Index, e, weights1, aCell.Index);
        TriangulateEdgeFan(center, m, aCell.Index);

        if (!aCell.IsUnderwater)
        {
            bool reversed = aCell.HasIncomingRiver;
            Vector3 indices;
            indices.x = indices.y = indices.z = aCell.Index;

            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, aCell.RiverSurfaceY, 0.6f, reversed, indices);

            center.y = m.v2.y = m.v4.y = aCell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed)
            {
                rivers.AddTriangleUV(new Vector2(.5f, .4f), new Vector2(1f, .2f), new Vector2(0f, .2f));
            }
            else
            {
                rivers.AddTriangleUV(new Vector2(.5f, .4f), new Vector2(0f, .6f), new Vector2(1f, .6f));
            }

            rivers.AddTriangleCellData(indices, weights1);
        }
    }

    private void TriangulateAdjacentToRiver(HexDirection aDir, HexCell aCell, Vector3 center, EdgeVertices e)
    {
        if(aCell.HasRoads)
        {
            TriangulateRoadAdjacentToRiver(aDir, aCell, center, e);
        }

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

        TriangulateEdgeStrip(m, weights1, aCell.TerrainTypeIndex, e, weights1, aCell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, aCell.Index);

        if (!aCell.IsUnderwater && !aCell.HasRoadThroughEdge(aDir))
            features.AddFeature(aCell, (center + e.v1 + e.v5) * (1f / 3f));
    }

    private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed, Vector3 indices)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed, indices);
    }

    private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed, Vector3 indices)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        rivers.AddQuad(v1, v2, v3, v4);
        if(reversed)
            rivers.AddQuadUV(1f, 0f, .8f - v, .6f - v);
        else
            rivers.AddQuadUV(0f, 1f, v, v +.2f);

        rivers.AddQuadCellData(indices, weights1, weights2);
    }

    private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR, float index)
    {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        Vector3 indices;
        indices.x = indices.y = indices.z = index;
        roads.AddTriangleCellData(indices, weights1);
    }

    private void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6, Color w1, Color w2, Vector3 indices)
    {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);
        roads.AddQuadUV(0f, 1f, 0f, 0f);
        roads.AddQuadUV(1f, 0f, 0f, 0f);
        roads.AddQuadCellData(indices, w1, w2);
        roads.AddQuadCellData(indices, w1, w2);
    }

    private void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, bool hasRoadThroughCellEdge, float index)
    {
        if (hasRoadThroughCellEdge)
        {
            Vector3 indices;
            indices.x = indices.y = indices.z = index;

            Vector3 mC = Vector3.Lerp(mL, mR, .5f);
            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4, weights1, weights1, indices);

            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);

            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));

            roads.AddTriangleCellData(indices, weights1);
            roads.AddTriangleCellData(indices, weights1);
        }
        else
            TriangulateRoadEdge(center, mL, mR, index);
    }

    private void TriangulateRoadAdjacentToRiver(HexDirection aDir, HexCell aCell, Vector3 center, EdgeVertices e)
    {
        bool hasRoadThroughCellEdge = aCell.HasRoadThroughEdge(aDir);
        bool previousHasRiver = aCell.HasRiverThroughEdge(aDir.Previous());
        bool nextHasRiver = aCell.HasRiverThroughEdge(aDir.Next());
        Vector2 interpolators = GetRoadInterpolators(aDir, aCell);
        Vector3 roadCenter = center;

        if (aCell.HasRiverBeginOrEnd)
        {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(aCell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        else if (aCell.IncomingRiver == aCell.OutgoingRiver.Opposite())
        {
            Vector3 corner;
            if (previousHasRiver)
            {
                if (!hasRoadThroughCellEdge && !aCell.HasRoadThroughEdge(aDir.Next()))
                    return;

                corner = HexMetrics.GetSecondSolidCorner(aDir);
            }
            else
            {
                if (!hasRoadThroughCellEdge && !aCell.HasRoadThroughEdge(aDir.Previous()))
                    return;

                corner = HexMetrics.GetFirstSolidCorner(aDir);
            }

            roadCenter += corner * .5f;
            if (aCell.IncomingRiver == aDir.Next() &&
                aCell.HasRoadThroughEdge(aDir.Next2()) ||
                aCell.HasRoadThroughEdge(aDir.Opposite()))
            {
                features.AddBridge(roadCenter, center - corner * .5f);
            }
            center += corner * .25f;
        }
        else if (aCell.IncomingRiver == aCell.OutgoingRiver.Previous())
        {
            roadCenter -= HexMetrics.GetSecondCorner(aCell.IncomingRiver) * .2f;
        }
        else if (aCell.IncomingRiver == aCell.OutgoingRiver.Next())
        {
            roadCenter -= HexMetrics.GetFirstCorner(aCell.IncomingRiver) * .2f;
        }
        else if (previousHasRiver && nextHasRiver)
        {
            if (!hasRoadThroughCellEdge)
                return;

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(aDir) * HexMetrics.InnerToOuter;
            roadCenter += offset * .7f;
            center += offset * .5f;
        }
        else
        {
            HexDirection middle;
            if (previousHasRiver)
                middle = aDir.Next();
            else if (nextHasRiver)
                middle = aDir.Previous();
            else
                middle = aDir;

            if (
                !aCell.HasRoadThroughEdge(middle) &&
                !aCell.HasRoadThroughEdge(middle.Previous()) &&
                !aCell.HasRoadThroughEdge(middle.Next()))
            {
                return;
            }

            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offset * .25f;
            if (aDir == middle && aCell.HasRoadThroughEdge(aDir.Opposite()))
            {
                features.AddBridge(roadCenter, center - offset * (HexMetrics.InnerToOuter * .7f));
            }
        }


        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughCellEdge, aCell.Index);

        if(previousHasRiver)
        {
            TriangulateRoadEdge(roadCenter, center, mL, aCell.Index);
        }
        if(nextHasRiver)
        {
            TriangulateRoadEdge(roadCenter, mR, center, aCell.Index);
        }
    }

    private void TriangulateWater(HexDirection aDir, HexCell aCell, Vector3 center)
    {
        center.y = aCell.WaterSurfaceY;

        HexCell neighbor = aCell.GetNeighbor(aDir);
        if (neighbor != null && !neighbor.IsUnderwater)
        {
            TriangulateWaterShore(aDir, aCell, neighbor, center);
        }
        else
        {
            TriangulateOpenWater(aDir, aCell, neighbor, center);
        }
    }

    private void TriangulateOpenWater(HexDirection aDir, HexCell aCell, HexCell neighbor, Vector3 center)
    {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(aDir);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(aDir);

        water.AddTriangle(center, c1, c2);
        Vector3 indices;
        indices.x = indices.y = indices.z = aCell.Index;
        water.AddTriangleCellData(indices, weights1);

        if (aDir <= HexDirection.SE && neighbor != null)
        {
            Vector3 bridge = HexMetrics.GetWaterBridge(aDir);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);
            indices.y = neighbor.Index;
            water.AddQuadCellData(indices, weights1, weights2);

            if (aDir <= HexDirection.E)
            {
                HexCell nextNeighbor = aCell.GetNeighbor(aDir.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                    return;

                water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(aDir.Next()));
                indices.z = nextNeighbor.Index;
                water.AddTriangleCellData(indices, weights1, weights2, weights3);
            }
        }
    }

    private void TriangulateWaterShore(HexDirection aDir, HexCell aCell, HexCell neighbor, Vector3 center)
    {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(aDir),
            center + HexMetrics.GetSecondWaterCorner(aDir)
            );

        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        Vector3 indices;
        indices.x = indices.z = aCell.Index;
        indices.y = neighbor.Index;
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);

        Vector3 center2 = neighbor.Position;
        center2.y = center.y;

        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(aDir.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(aDir.Opposite())
            );

        if (aCell.HasRiverThroughEdge(aDir))
        {
            TriangulateEstuary(e1, e2, aCell.IncomingRiver == aDir, indices);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
            waterShore.AddQuadCellData(indices, weights1, weights2);
        }

        HexCell nextNeighbor = aCell.GetNeighbor(aDir.Next());
        if(nextNeighbor != null)
        {
            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(aDir.Previous()) : 
                HexMetrics.GetFirstSolidCorner(aDir.Previous()));
            v3.y = center.y;

            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f));

            indices.z = nextNeighbor.Index;
            waterShore.AddTriangleCellData(indices, weights1, weights2, weights3);
        }
    }

    private void TriangulateWaterfallInWater(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float waterY, Vector3 indices)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);

        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, .8f, 1f);
        rivers.AddQuadCellData(indices, weights1, weights2);
    }

    private void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver, Vector3 indices)
    {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);
        waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);

        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), 
            new Vector2(1f, 1f), new Vector2(0f, 0f)
            );
        estuaries.AddTriangleUV(
            new Vector2(0f, 0f), 
            new Vector2(1f, 1f), 
            new Vector2(1f, 1f)
            );
        estuaries.AddQuadUV(
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
            );

        estuaries.AddQuadCellData(indices, weights2, weights1, weights2, weights1);
        estuaries.AddTriangleCellData(indices, weights1, weights2, weights2);
        estuaries.AddQuadCellData(indices, weights1, weights2);

        if (incomingRiver)
        {
            estuaries.AddQuadUV2(
                new Vector2(1.5f, 1f), new Vector2(.7f, 1.15f),
                new Vector2(1f, .8f), new Vector2(.5f, 1.1f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(.5f, 1.1f),
                new Vector2(1f, .8f),
                new Vector2(0f, .8f)
                );
            estuaries.AddQuadUV2(
                new Vector2(.5f, 1.1f), new Vector2(.3f, 1.15f),
                new Vector2(0f, .8f), new Vector2(-.5f, 1f)
                );
        }
        else
        {
            estuaries.AddQuadUV2(
                new Vector2(-.5f, -.2f), new Vector2(.3f, -.35f),
                new Vector2(0f, 0f), new Vector2(.5f, -.3f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(.5f, -.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
                );
            estuaries.AddQuadUV2(
                new Vector2(.5f, -.3f), new Vector2(.7f, -.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -.2f)
                );
        }
    }

    private Vector2 GetRoadInterpolators(HexDirection aDir, HexCell aCell)
    {
        Vector2 interpolators = new Vector2();
        if (aCell.HasRoadThroughEdge(aDir))
            interpolators.x = interpolators.y = .5f;
        else
        {
            interpolators.x = aCell.HasRoadThroughEdge(aDir.Previous()) ? .5f : .25f;
            interpolators.y = aCell.HasRoadThroughEdge(aDir.Next()) ? .5f : .25f;
        }
        return interpolators;
    }
}
