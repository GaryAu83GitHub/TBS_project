using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;


public class HexCell : MonoBehaviour
{
    public HexCoordinates Coordinates;

    public Color Color;

    [SerializeField]
    HexCell[] Neighbors;

    public RectTransform UIRect;

    public Vector3 Position { get { return transform.localPosition; } }

    public int Elevation 
    {
        get { return myElavation; } 
        set 
        { 
            myElavation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = UIRect.localPosition;
            uiPosition.z = -position.y;
            UIRect.localPosition = uiPosition;
        } 
    }
    private int myElavation;

    public HexCell GetNeighbor(HexDirection aDir)
    {
        return Neighbors[(int)aDir];
    }

    public void SetNeighbor(HexDirection aDir, HexCell aCell)
    {
        Neighbors[(int)aDir] = aCell;
        aCell.Neighbors[(int)aDir.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection aDir)
    {
        return HexMetrics.GetEdgeType(Elevation, Neighbors[(int)aDir].Elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);
    }
}
