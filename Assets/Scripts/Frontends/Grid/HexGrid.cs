using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;
using Assets.Scripts.Backends.Tools;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class HexGrid : MonoBehaviour
{
    [SerializeField]
    public int cellCountX = 20, cellCountZ = 15;

    public HexCell cellPrefab;
    public Text cellLabelPrefab;

    public HexGridChunk chunkPrefab;

    public HexUnit unitPrefab;

    public Texture2D noiseSource;

    public int seed;
    
    private HexGridChunk[] myChunks;
    private HexCell[] cells;

    private int chunkCountX, chunkCountZ;

    private HexCellPriorityQueue searchFrontier;

    private int searchFrontierPhase;

    private HexCell currentPathFrom, currentPathTo;
    public bool HasPath { get { return currentPathExists; } }

    private bool currentPathExists;

    private List<HexUnit> units = new List<HexUnit>();

    private HexCellShaderData cellShaderData;

    void Awake()
    {
        HexMetrics.NoiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexUnit.unitPrefab = unitPrefab;
        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        CreateMap(cellCountX, cellCountZ);
    }

    private void OnEnable()
    {
        if (!HexMetrics.NoiseSource)
        {
            HexMetrics.NoiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
        }
    }

    public HexCell GetCell(Vector3 aPosition)
    {
        aPosition = transform.InverseTransformPoint(aPosition);
        HexCoordinates coordinates = HexCoordinates.FromPosition(aPosition);

        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if(z < 0 || z >= cellCountZ)
            return null;

        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
            return null;

        return cells[x + z * cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for(int i = 0; i < myChunks.Length; i++)
        {
            myChunks[i].ShowUI(visible);
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);

        for(int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        writer.Write(units.Count);
        for(int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        ClearUnits();

        int x = 20, z = 15;
        if(header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        if (x != cellCountX || z != cellCountZ)
        {
            if (!CreateMap(x, z))
                return;
        }

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader, header);
        }
        for(int i = 0; i < myChunks.Length; i++)
        {
            myChunks[i].Refresh();
        }

        if (header >= 2)
        {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }
    }

    public bool CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.ChunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.ChunkSizeZ != 0)
        {
            Debug.LogError("Unsupported map size");
            return false;
        }

        ClearPath();
        ClearUnits();

        if(myChunks != null)
        {
            for(int i = 0; i < myChunks.Length; i++)
            {
                Destroy(myChunks[i].gameObject);
            }
        }
        
        cellCountX = x;
        cellCountZ = z;

        chunkCountX = cellCountX / HexMetrics.ChunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.ChunkSizeZ;

        cellShaderData.Initialize(cellCountX, cellCountZ);

        CreateChunks();
        CreateCells();

        return true;
    }

    public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell, unit);
        ShowPath(unit.Speed);
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Grid = this;
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }

        return null;
    }

    public void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;

            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            current.DisableHighlight();
            currentPathExists = false;
        }
        else if (currentPathFrom)
        {
            currentPathFrom.DisableHighlight();
            currentPathTo.DisableHighlight();
        }
        currentPathFrom = currentPathTo = null;
    }

    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
            return null;

        List<HexCell> path = ListPool<HexCell>.Get();
        for(HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }
        path.Add(currentPathFrom);
        path.Reverse();
        return path;
    }

    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibilityCells(fromCell, range);
        for(int i = 0; i < cells.Count; i++)
            cells[i].IncreaseVisibility();

        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibilityCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
            cells[i].DecreaseVisibility();

        ListPool<HexCell>.Add(cells);
    }

    private void HandleInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
            TouchCell(hit.point);
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * .5f - z / 2) * (HexMetrics.InnerRadius * 2);
        position.y = 0f;
        position.z = z * (HexMetrics.OuterRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Index = i;
        cell.ShaderData = cellShaderData;
        
        if(x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }

        if(z > 0) 
        {
            if((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                
                if(x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);

                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);

        cell.UIRect = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, HexCell aCell)
    {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;

        HexGridChunk chunk = myChunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, aCell);
    }

    private void CreateChunks()
    {
        myChunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for(int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for(int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = myChunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void TouchCell(Vector3 aPosition)
    {
        aPosition = transform.InverseTransformPoint(aPosition);
        HexCoordinates coordinates = HexCoordinates.FromPosition(aPosition);
        
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        HexCell cell = cells[index];
    }

    private IEnumerator Search(HexCell fromCell)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Distance = int.MaxValue;
        }

        WaitForSeconds delay = new WaitForSeconds(1 / 60f);
        List<HexCell> frontier = new List<HexCell>();
        fromCell.Distance = 0;
        frontier.Add(fromCell);
        while (frontier.Count > 0)
        {
            yield return delay;
            HexCell current = frontier[0];
            frontier.RemoveAt(0);
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null)
                    continue;
                if (neighbor.IsUnderwater)
                    continue;

                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.CLIFF)
                    continue;

                int distance = current.Distance;
                if (current.HasRoadThroughEdge(d))
                {
                    distance += 1;
                }
                else if (current.Walled != neighbor.Walled)
                    continue;
                else
                {
                    distance += edgeType == HexEdgeType.FLAT ? 5 : 10;
                    distance += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                }

                if (neighbor.Distance == int.MaxValue)
                {
                    neighbor.Distance = distance;
                    frontier.Add(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    neighbor.Distance = distance;
                }


                frontier.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            }
        }
    }

    private bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        int speed = unit.Speed;

        searchFrontierPhase += 2;

        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase)
                    continue;
                //if (neighbor.IsUnderwater || neighbor.Unit)
                //    continue;

                //HexEdgeType edgeType = current.GetEdgeType(neighbor);
                //if (edgeType == HexEdgeType.CLIFF)
                //    continue;

                //int moveCost;
                //if (current.HasRoadThroughEdge(d))
                //{
                //    moveCost = 1;
                //}
                //else if (current.Walled != neighbor.Walled)
                //    continue;
                //else
                //{
                //    moveCost = edgeType == HexEdgeType.FLAT ? 5 : 10;
                //    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                //}

                if (!unit.IsValidDestination(neighbor))
                    continue;

                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if (moveCost < 0)
                    continue;

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;
                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.Coordinates.DistanceTo(toCell.Coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return false;
    }

    private void ShowPath(int speed)
    {
        if(currentPathExists)
        {
            HexCell current = currentPathTo;

            while(current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.gray);
                current = current.PathFrom;
            }
        }
        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.red);
    }

    private List<HexCell> GetVisibilityCells(HexCell fromCell, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();

        searchFrontierPhase += 2;
        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;

            visibleCells.Add(current);
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase)
                    continue;

                int distance = current.Distance + 1;
                if (distance > range)
                    continue;
                
                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return visibleCells;
    }

    private void ClearUnits()
    {
        for(int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }
}
