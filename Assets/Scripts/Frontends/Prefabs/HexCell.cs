using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;
using Assets.Scripts.Frontends.ExtendingTools;
using System.IO;

public class HexCell : MonoBehaviour
{
    public HexCoordinates Coordinates;    


    [SerializeField]
    public RectTransform UIRect;

    [SerializeField]
    public HexGridChunk Chunk;

    public Vector3 Position { get { return transform.localPosition; } }

    // terrain stuff
    public int TerrainTypeIndex
    {
        get { return terrainTypeIndex; }
        set
        {
            if(terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                //Refresh();
                ShaderData.RefreshTerrain(this);
            }
        }
    }

    private int terrainTypeIndex;

    public int Elevation 
    {
        get { return elevation; } 
        set 
        { 
            if(elevation == value)
                return;

            elevation = value;
            RefreshPosition();
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

    // path stuff
    public int Distance
    {
        get { return distance; }
        set { distance = value; }
    }

    private int distance;

    public HexCell PathFrom { get; set; }

    public int SearchHeuristic { get; set; }

    public int SearchPriority 
    {
        get { return distance + SearchHeuristic; }
    }

    public HexCell NextWithSamePriority { get; set; }

    public int SearchPhase { get; set; }

    // unit stuff
    public HexUnit Unit { get; set; }

    public HexCellShaderData ShaderData { get; set; }

    public int Index { get; set; }

    // visibility and exploring stuff
    public bool IsVisible { get { return visibility > 0; } }
    private int visibility;

    public bool IsExplored { get; private set; }

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

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;

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

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);
        writer.Write(walled);

        if (hasIncomingRiver)
            writer.Write((byte)(incomingRiver + 128));
        else
            writer.Write((byte)0);

        if (hasOutgoingRiver)
            writer.Write((byte)(outgoingRiver + 128));
        else
            writer.Write((byte)0);

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++)
        {
            if(roads[i])
                roadFlags |= 1 << i;
        }
        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        elevation = reader.ReadByte();
        RefreshPosition();
        waterLevel = reader.ReadByte();
        urbanLevel = reader.ReadByte();
        farmLevel = reader.ReadByte();
        plantLevel = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else
            hasIncomingRiver = false;

        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
            hasOutgoingRiver = false;

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
            roads[i] = (roadFlags & (1 << i)) != 0;
    }

    public void DisableHighlight()
    {
        Image highlight = UIRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color)
    {
        Image highlight = UIRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    public void SetLabel(string text)
    {
        Text label = UIRect.GetComponent<Text>();
        label.text = text;
    }

    public void IncreaseVisibility()
    {
        visibility += 1;
        if (visibility == 1)
        {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }
    public void DecreaseVisibility()
    {
        visibility -= 1;
        if (visibility == 0)
            ShaderData.RefreshVisibility(this);
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

            if(Unit)
            {
                Unit.ValidateLocation();
            }
        }
    }

    private void RefreshSelfOnly()
    {
        Chunk.Refresh();
        if (Unit)
        {
            Unit.ValidateLocation();
        }
    }

    private void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.ElevationStep;
        position.y += 
            (HexMetrics.SampleNoise(position).y * 2f - 1f) * 
            HexMetrics.ElevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = UIRect.localPosition;
        uiPosition.z = -position.y;
        UIRect.localPosition = uiPosition;
    }

    //private void UpdateDistanceLabel()
    //{
    //    Text label = UIRect.GetComponent<Text>();
    //    label.text = distance == int.MaxValue ? "" : distance.ToString();
    //    label.fontSize = distance < 100 ? 8 : 6;
    //}
}
