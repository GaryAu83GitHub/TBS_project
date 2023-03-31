using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;
using Assets.Scripts.Frontends.ExtendingTools;

public class HexCell : MonoBehaviour
{
    public HexCoordinates Coordinates;    


    [SerializeField]
    public RectTransform UIRect;

    [SerializeField]
    public HexGridChunk Chunk;

    public Vector3 Position { get { return transform.localPosition; } }

    public Color Color 
    {
        get { return color; }
        set 
        {
            if (color == value)
                return;

            color = value;
            Refresh();
        }
    }
    [SerializeField]
    private Color color;

    public int Elevation 
    {
        get { return elavation; } 
        set 
        { 
            if(elavation == value)
                return;

            elavation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = UIRect.localPosition;
            uiPosition.z = -position.y;
            UIRect.localPosition = uiPosition;

            if (myHasOutgoingRiver && elavation < GetNeighbor(myOutgoingRiver).elavation)
                RemoveOutgoingRiver();

            if (myHasIncomingRiver && elavation < GetNeighbor(myIncomingRiver).elavation)
                RemoveIncomingRiver();

            Refresh();
        } 
    }
    [SerializeField]
    private int elavation = int.MinValue;

    // river stuffs
    public bool HasIncomingRiver { get { return myHasIncomingRiver; } }
    public bool HasOutgoingRiver { get { return myHasOutgoingRiver; } }

    public bool HasRiver { get { return myHasIncomingRiver || myHasOutgoingRiver; } }
    public bool HasRiverBeginOrEnd { get { return myHasIncomingRiver != myHasOutgoingRiver; } }
        
    [SerializeField]
    private bool myHasIncomingRiver, myHasOutgoingRiver;
    
    public HexDirection IncomingRiver { get { return myIncomingRiver; } }
    public HexDirection OutgoingRiver { get { return myOutgoingRiver; } }
    
    [SerializeField]
    private HexDirection myIncomingRiver, myOutgoingRiver;

    public float StreamBedY 
    { 
        get 
        {
            return (elavation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep;
        } 
    }
    public float RiverSurfaceY 
    { 
        get 
        { 
            return (elavation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep; 
        } 
    }

    // neighbors stuffs
    [SerializeField]
    HexCell[] neighbors;

    // roads stuff
    public bool HasRoads 
    {
        get 
        {
            for(int i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                    return true;
            }
            return false;
        }
    }

    [SerializeField]
    bool[] roads;

    public HexCell GetNeighbor(HexDirection aDir)
    {
        return neighbors[(int)aDir];
    }

    public void SetNeighbor(HexDirection aDir, HexCell aCell)
    {
        neighbors[(int)aDir] = aCell;
        aCell.neighbors[(int)aDir.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection aDir)
    {
        return HexMetrics.GetEdgeType(Elevation, neighbors[(int)aDir].Elevation);
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

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void RemoveOutgoingRiver()
    {
        if (!myHasOutgoingRiver)
            return;

        myHasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(myOutgoingRiver);
        neighbor.myHasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!myHasIncomingRiver)
            return;

        myHasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(myIncomingRiver);
        neighbor.myHasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (myHasOutgoingRiver && myOutgoingRiver == direction)
            return;

        HexCell neighbor = GetNeighbor(direction);
        if (!neighbor || elavation < neighbor.elavation)
            return;

        RemoveOutgoingRiver();
        if (myHasIncomingRiver && myIncomingRiver == direction)
            RemoveIncomingRiver();

        myHasOutgoingRiver = true;
        myOutgoingRiver = direction;
        RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();
        neighbor.myHasIncomingRiver = true;
        neighbor.myIncomingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
    }

    public bool HasRoadThroughEdge(HexDirection aDir)
    {
        return roads[(int)aDir];
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                roads[i] = false;
                neighbors[i].roads[(int)((HexDirection)i).Opposite()] = false;
                neighbors[i].RefreshSelfOnly();
                RefreshSelfOnly();
            }
        } 
    }

    private void Refresh()
    {
        if (Chunk)
        {
            Chunk.Refresh();

            for(int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.Chunk != Chunk)
                    neighbor.Chunk.Refresh();
            }
        }
    }

    private void RefreshSelfOnly()
    {
        Chunk.Refresh();
    }
}
