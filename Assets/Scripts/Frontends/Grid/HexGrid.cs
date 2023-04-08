using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;
using System.IO;

public class HexGrid : MonoBehaviour
{
    [SerializeField]
    public int cellCountX = 20, cellCountZ = 15;

    public HexCell cellPrefab;
    public Text cellLabelPrefab;

    public HexGridChunk chunkPrefab;

    public Texture2D noiseSource;

    public int seed;
    
    public Color[] colors;

    private HexGridChunk[] myChunks;
    private HexCell[] myCells;

    private int chunkCountX, chunkCountZ;

    void Awake()
    {
        HexMetrics.NoiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexMetrics.Colors = colors;
        CreateMap(cellCountX, cellCountZ);
    }

    private void OnEnable()
    {
        if (!HexMetrics.NoiseSource)
        {
            HexMetrics.NoiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexMetrics.Colors = colors;
        }
    }

    public HexCell GetCell(Vector3 aPosition)
    {
        aPosition = transform.InverseTransformPoint(aPosition);
        HexCoordinates coordinates = HexCoordinates.FromPosition(aPosition);

        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return myCells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        if(z < 0 || z >= cellCountZ)
            return null;

        int x = coordinates.X + z / 2;
        if (x < 0 || x >= cellCountX)
            return null;

        return myCells[x + z * cellCountX];
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

        for(int i = 0; i < myCells.Length; i++)
        {
            myCells[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader)
    {
        CreateMap(reader.ReadInt32(), reader.ReadInt32());

        for (int i = 0; i < myCells.Length; i++)
        {
            myCells[i].Load(reader);
        }
        for(int i = 0; i < myChunks.Length; i++)
        {
            myChunks[i].Refresh();
        }
    }

    public void CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.ChunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.ChunkSizeZ != 0)
        {
            Debug.LogError("Unsupported map size");
            return;
        }

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
        CreateChunks();
        CreateCells();
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

        HexCell cell = myCells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        
        if(x > 0)
        {
            cell.SetNeighbor(HexDirection.W, myCells[i - 1]);
        }

        if(z > 0) 
        {
            if((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, myCells[i - cellCountX]);
                
                if(x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, myCells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, myCells[i - cellCountX]);

                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, myCells[i - cellCountX + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        //label.rectTransform.SetParent(myGridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.Coordinates.ToStringOnSeperateLines();

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
        myCells = new HexCell[cellCountZ * cellCountX];

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
        HexCell cell = myCells[index];

        //if(cell.Color == DefaultColor)
        //    cell.Color = TouchedColor;
        //else
        //    cell.Color = DefaultColor;
        
    }
}
