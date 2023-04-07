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

    // terrain stuff
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
        get { return elevation; } 
        set 
        { 
            if(elevation == value)
                return;

            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = UIRect.localPosition;
            uiPosition.z = -position.y;
            UIRect.localPosition = uiPosition;

            //if (myHasOutgoingRiver && elevation < GetNeighbor(myOutgoingRiver).elevation)
            //    RemoveOutgoingRiver();

            //if (myHasIncomingRiver && elevation < GetNeighbor(myIncomingRiver).elevation)
            //    RemoveIncomingRiver();
            ValidateRivers();

            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                    SetRoad(i, false);
            }

            Refresh();
        } 
    }
    [SerializeField]
    private int elevation = int.MinValue;

    // water stuff
    public bool IsUnderwater { get { return waterLevel > elevation; } }

    public float WaterSurfaceY { get { return (waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep; } }

    public int WaterLevel
    {
        get { return waterLevel; }
        set
        {
            if (waterLevel == value)
                return;

            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }
    [SerializeField]
    private int waterLevel;

    // river stuffs
    public bool HasIncomingRiver { get { return hasIncomingRiver; } }
    public bool HasOutgoingRiver { get { return hasOutgoingRiver; } }

    public bool HasRiver { get { return hasIncomingRiver || hasOutgoingRiver; } }
    public bool HasRiverBeginOrEnd { get { return hasIncomingRiver != hasOutgoingRiver; } }
        
    [SerializeField]
    private bool hasIncomingRiver, hasOutgoingRiver;
    
    public HexDirection IncomingRiver { get { return incomingRiver; } }
    public HexDirection OutgoingRiver { get { return outgoingRiver; } }

    public HexDirection RiverBeginOrEndDirection { get { return hasIncomingRiver ? incomingRiver : outgoingRiver; } }
    
    [SerializeField]
    private HexDirection incomingRiver, outgoingRiver;

    public float StreamBedY { get { return (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep; } }
    public float RiverSurfaceY { get { return (elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep; } }

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

    // feature stuffs
    public int UrbanLevel
    {
        get { return urbanLevel; }
        set
        {
            if(urbanLevel != value)
            {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get { return farmLevel; }
        set
        {
            if(farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get { return plantLevel; }
        set
        {
            if(plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    private int urbanLevel, farmLevel, plantLevel;

    public bool Walled 
    {
        get { return walled; }
        set
        {
            if(walled != value)
            {
                walled = value;
                Refresh();
            }
        }
    }

    private bool walled;

    public int SpecialIndex 
    {
        get { return specialIndex; }
        set
        {
            if (specialIndex != value && !HasRiver)
            {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    public bool IsSpecial { get { return specialIndex > 0; } }

    private int specialIndex;

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

    public int GetElevationDifference(HexDirection aDir)
    {
        int difference = elevation - GetNeighbor(aDir).elevation;
        return difference >= 0 ? difference : -difference;
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
            return;

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
            return;

        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)
            return;

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
            return;

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction)
            RemoveIncomingRiver();

        hasOutgoingRiver = true;
        outgoingRiver = direction;
        specialIndex = 0;
        //RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;
        //neighbor.RefreshSelfOnly();

        SetRoad((int)direction, false);
    }

    public bool HasRoadThroughEdge(HexDirection aDir)
    {
        return roads[(int)aDir];
    }

    public void AddRoad(HexDirection aDir)
    {
        if (!roads[(int)aDir] && !HasRiverThroughEdge(aDir) && !IsSpecial && !GetNeighbor(aDir).IsSpecial && GetElevationDifference(aDir) <= 1)
            SetRoad((int)aDir, true);

    }

    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i])
            {
                SetRoad(i, false);
            }
        } 
    }

    private bool IsValidRiverDestination(HexCell neighbor)
    {   
        return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }

    private void ValidateRivers()
    {
        if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver)))
        {
            RemoveOutgoingRiver();
        }
        if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }

    private void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
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
