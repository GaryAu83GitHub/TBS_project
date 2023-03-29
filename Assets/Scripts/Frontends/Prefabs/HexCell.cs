using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;


public class HexCell : MonoBehaviour
{
    public HexCoordinates Coordinates;    

    [SerializeField]
    HexCell[] Neighbors;

    [HideInInspector]
    public RectTransform UIRect;

    [HideInInspector]
    public HexGridChunk Chunk;

    public Vector3 Position { get { return transform.localPosition; } }

    public Color Color 
    {
        get { return myColor; }
        set 
        {
            if (myColor == value)
                return;

            myColor = value;
            Refresh();
        }
    }
    private Color myColor;

    public int Elevation 
    {
        get { return myElavation; } 
        set 
        { 
            if(myElavation == value)
                return;

            myElavation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = UIRect.localPosition;
            uiPosition.z = -position.y;
            UIRect.localPosition = uiPosition;

            Refresh();
        } 
    }
    private int myElavation = int.MinValue;

    public bool HasIncomingRiver { get { return myHasIncomingRiver; } }
    public bool HasOutgoingRiver { get { return myHasOutgoingRiver; } }

    public bool HasRiver { get { return myHasIncomingRiver || myHasOutgoingRiver; } }
    public bool HasRiverBeginOrEnd { get { return myHasIncomingRiver != myHasOutgoingRiver; } }

    private bool myHasIncomingRiver, myHasOutgoingRiver;
    
    public HexDirection IncomingRiver { get { return myIncomingRiver; } }
    public HexDirection OutgoingRiver { get { return myOutgoingRiver; } }

    private HexDirection myIncomingRiver, myOutgoingRiver;

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

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            myHasIncomingRiver && myIncomingRiver == direction ||
            myHasOutgoingRiver && myOutgoingRiver == direction;
    }

    private void Refresh()
    {
        if (Chunk)
        {
            Chunk.Refresh();

            for(int i = 0; i < Neighbors.Length; i++)
            {
                HexCell neighbor = Neighbors[i];
                if (neighbor != null && neighbor.Chunk != Chunk)
                    neighbor.Chunk.Refresh();
            }
        }
    }
}
