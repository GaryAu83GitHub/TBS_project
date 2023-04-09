using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;
using Assets.Scripts.Backends.Tools;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    public bool useCollider, useColors, useUVCoordinates, useUV2Coordinates;
    public bool useTerrainTypes;

    private Mesh myHexMesh;
    private MeshCollider myCollider;

    [NonSerialized] List<Vector3> myVertices, terrainTypes;
    [NonSerialized] List<Color> myColors;
    [NonSerialized] List<int> myTriangles;
    [NonSerialized] List<Vector2> myUVs, myUV2s;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = myHexMesh = new Mesh();
        if(useCollider)
            myCollider = gameObject.AddComponent<MeshCollider>();

        myHexMesh.name = "Hex Mesh";
    }

    public void Clear()
    {
        myHexMesh.Clear();
        myVertices = ListPool<Vector3>.Get();

        if(useColors)
            myColors = ListPool<Color>.Get();
        
        if(useUVCoordinates)
            myUVs = ListPool<Vector2>.Get();

        if (useUV2Coordinates)
            myUV2s = ListPool<Vector2>.Get();

        if (useTerrainTypes)
            terrainTypes = ListPool<Vector3>.Get();

        myTriangles = ListPool<int>.Get();
    }

    public void Apply()
    {
        myHexMesh.SetVertices(myVertices);
        ListPool<Vector3>.Add(myVertices);

        if (useColors)
        {
            myHexMesh.SetColors(myColors);
            ListPool<Color>.Add(myColors);
        }

        if(useUVCoordinates)
        {
            myHexMesh.SetUVs(0, myUVs);
            ListPool<Vector2>.Add(myUVs);
        }

        if(useUV2Coordinates)
        {
            myHexMesh.SetUVs(1, myUV2s);
            ListPool<Vector2>.Add(myUV2s);
        }

        if(useTerrainTypes)
        {
            myHexMesh.SetUVs(2, terrainTypes);
            ListPool<Vector3>.Add(terrainTypes);
        }

        myHexMesh.SetTriangles(myTriangles, 0);
        ListPool<int>.Add(myTriangles);
        myHexMesh.RecalculateNormals();
        if(useCollider)
            myCollider.sharedMesh = myHexMesh;
    }

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = myVertices.Count;

        myVertices.Add(HexMetrics.Perturb(v1));
        myVertices.Add(HexMetrics.Perturb(v2));
        myVertices.Add(HexMetrics.Perturb(v3));

        myTriangles.Add(vertexIndex);
        myTriangles.Add(vertexIndex + 1);
        myTriangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        int vertexIndex = myVertices.Count;

        myVertices.Add(v1);
        myVertices.Add(v2);
        myVertices.Add(v3);

        myTriangles.Add(vertexIndex);
        myTriangles.Add(vertexIndex + 1);
        myTriangles.Add(vertexIndex + 2);
    }

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        int vertexIndex = myVertices.Count;

        myVertices.Add(HexMetrics.Perturb(v1));
        myVertices.Add(HexMetrics.Perturb(v2));
        myVertices.Add(HexMetrics.Perturb(v3));
        myVertices.Add(HexMetrics.Perturb(v4));

        myTriangles.Add(vertexIndex);
        myTriangles.Add(vertexIndex + 2);
        myTriangles.Add(vertexIndex + 1);
        myTriangles.Add(vertexIndex + 1);
        myTriangles.Add(vertexIndex + 2);
        myTriangles.Add(vertexIndex + 3);
    }

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
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

    public void AddTriangleColor(Color aColor)
    {
        myColors.Add(aColor);
        myColors.Add(aColor);
        myColors.Add(aColor);
    }

    public void AddTriangleColor(Color aColor1, Color aColor2, Color aColor3)
    {
        myColors.Add(aColor1);
        myColors.Add(aColor2);
        myColors.Add(aColor3);
    }

    public void AddTriangleTerrainTypes(Vector3 types)
    {
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        myUVs.Add(uv1);
        myUVs.Add(uv2);
        myUVs.Add(uv3);
    }

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        myUV2s.Add(uv1);
        myUV2s.Add(uv2);
        myUV2s.Add(uv3);
    }

    public void AddQuadColor(Color aColor)
    {
        myColors.Add(aColor);
        myColors.Add(aColor);
        myColors.Add(aColor);
        myColors.Add(aColor);
    }

    public void AddQuadColor(Color aColor1, Color aColor2)
    {
        myColors.Add(aColor1);
        myColors.Add(aColor1);
        myColors.Add(aColor2);
        myColors.Add(aColor2);
    }

    public void AddQuadColor(Color aColor1, Color aColor2, Color aColor3, Color aColor4)
    {
        myColors.Add(aColor1);
        myColors.Add(aColor2);
        myColors.Add(aColor3);
        myColors.Add(aColor4);
    }

    public void AddQuadTerrainTypes(Vector3 types)
    {
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
        terrainTypes.Add(types);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        myUVs.Add(uv1);
        myUVs.Add(uv2);
        myUVs.Add(uv3);
        myUVs.Add(uv4);
    }

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        myUV2s.Add(uv1);
        myUV2s.Add(uv2);
        myUV2s.Add(uv3);
        myUV2s.Add(uv4);
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        myUVs.Add(new Vector2(uMin, vMin));
        myUVs.Add(new Vector2(uMax, vMin));
        myUVs.Add(new Vector2(uMin, vMax));
        myUVs.Add(new Vector2(uMax, vMax));
    }

    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        myUV2s.Add(new Vector2(uMin, vMin));
        myUV2s.Add(new Vector2(uMax, vMin));
        myUV2s.Add(new Vector2(uMin, vMax));
        myUV2s.Add(new Vector2(uMax, vMax));
    }
}