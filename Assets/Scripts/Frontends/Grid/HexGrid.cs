using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;

public class HexGrid : MonoBehaviour
{
    [SerializeField]
    private int ChunkCountX = 4, ChunkCountZ = 3;

    public Color DefaultColor = Color.white;
 
    public HexCell CellPrefab;
    public Text CellLabelPrefab;

    public HexGridChunk ChunkPrefab;

    public Texture2D NoiseSource;

    private HexGridChunk[] myChunks;
    private HexCell[] myCells;
    private HexMesh myHexMesh;
    
    private Canvas myGridCanvas;

    private int myCellCountX, myCellCountZ;

    void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;

        myHexMesh = GetComponentInChildren<HexMesh>();
        myGridCanvas = GetComponentInChildren<Canvas>();

        myCellCountX = ChunkCountX * HexMetrics.ChunkSizeX;
        myCellCountZ = ChunkCountZ * HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    private void OnEnable()
    {
        HexMetrics.NoiseSource = NoiseSource;
    }

    void Start()
    {
        myHexMesh.Triangulate(myCells);
    }

    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //    HandleInput();
    }

    public void ColorCell(Vector3 aPosition, Color aColor)
    {
        aPosition = transform.InverseTransformPoint(aPosition);
        HexCoordinates coordinates = HexCoordinates.FromPosition(aPosition);

        int index = coordinates.X + coordinates.Z * myCellCountX + coordinates.Z / 2;
        HexCell cell = myCells[index];
        cell.Color = aColor;
        myHexMesh.Triangulate(myCells);
    }

    public HexCell GetCell(Vector3 aPosition)
    {
        aPosition = transform.InverseTransformPoint(aPosition);
        HexCoordinates coordinates = HexCoordinates.FromPosition(aPosition);

        int index = coordinates.X + coordinates.Z * myCellCountX + coordinates.Z / 2;
        return myCells[index];
    }

    public void Refresh()
    {
        myHexMesh.Triangulate(myCells);
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

        HexCell cell = myCells[i] = Instantiate<HexCell>(CellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Color = DefaultColor;

        
        if(x > 0)
        {
            cell.SetNeighbor(HexDirection.W, myCells[i - 1]);
        }

        if(z > 0) 
        {
            if((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, myCells[i - myCellCountX]);
                
                if(x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, myCells[i - myCellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, myCells[i - myCellCountX]);

                if (x < myCellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, myCells[i - myCellCountX + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(CellLabelPrefab);
        label.rectTransform.SetParent(myGridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.Coordinates.ToStringOnSeperateLines();

        cell.UIRect = label.rectTransform;

        cell.Elevation = 0;
    }

    private void CreateChunks()
    {
        myChunks = new HexGridChunk[ChunkCountX * ChunkCountZ];

        for(int z = 0, i = 0; z < ChunkCountZ; z++)
        {
            for(int x = 0; x < ChunkCountX; x++)
            {
                HexGridChunk chunk = myChunks[i++] = Instantiate(ChunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCells()
    {
        myCells = new HexCell[myCellCountZ * myCellCountX];

        for (int z = 0, i = 0; z < myCellCountZ; z++)
        {
            for (int x = 0; x < myCellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void TouchCell(Vector3 aPosition)
    {
        aPosition = transform.InverseTransformPoint(aPosition);
        HexCoordinates coordinates = HexCoordinates.FromPosition(aPosition);
        
        int index = coordinates.X + coordinates.Z * myCellCountX + coordinates.Z / 2;
        HexCell cell = myCells[index];

        //if(cell.Color == DefaultColor)
        //    cell.Color = TouchedColor;
        //else
        //    cell.Color = DefaultColor;
        
        myHexMesh.Triangulate(myCells);
    }
}
