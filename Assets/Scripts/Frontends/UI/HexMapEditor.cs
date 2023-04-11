using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;
using System.IO;


public class HexMapEditor : MonoBehaviour
{
    private enum OptionalToggle { IGNORE, YES, NO }

    [SerializeField]
    public HexGrid hexGrid;

    [SerializeField]
    public Material terrainMaterial;

    [SerializeField]
    public HexUnit unitPrefab;

    private int myActiveTerrainTypeIndex;
    private int myActiveElevation;
    private int myActiveWaterLevel;
    private int myActiveUrbanLevel;
    private int myActiveFarmLevel;
    private int myActivePlantLevel;
    private int myActiveSpecialIndex;

    private bool myApplyElevation = true;
    private bool myApplyWaterLevel = true;
    private bool myApplyUrbanLevel = true;
    private bool myApplyFarmLevel = true;
    private bool myApplyPlantLevel = true;
    private bool myApplySpecialIndex = true;

    private int myBrushSize;

    private OptionalToggle myRiverMode, myRoadMode, myWalledMode;

    private bool isDrag;
    private bool editMode;

    private HexDirection dragDirection;
    private HexCell previousCell, searchFromCell, searchToCell;

    private void Awake()
    {
        terrainMaterial.DisableKeyword("GRID_ON");
    }

    void Update()
    {
        if(!EventSystem.current.IsPointerOverGameObject())
        {
            if(Input.GetMouseButton(0))
            {
                HandleInput();
                return;
            }
            if(Input.GetKeyDown(KeyCode.U))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    DestroyUnit();
                }
                else
                {
                    CreateUnit();
                }
                return;
            }
        }
        previousCell = null;
    }

    private void HandleInput()
    {
        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //RaycastHit hit;
        //if (Physics.Raycast(ray, out hit))
        HexCell currentCell = GetCellUnderCursor();
        if(currentCell)
        {   
            if (previousCell && previousCell != currentCell)
                ValidateDrag(currentCell);
            else
                isDrag = false;

            if (editMode)
                EditCells(currentCell);
            else if (Input.GetKey(KeyCode.LeftShift) && searchToCell != currentCell)
            {
                if (searchFromCell != currentCell)
                {
                    if (searchFromCell)
                        searchFromCell.DisableHighlight();
                    searchFromCell = currentCell;
                    searchFromCell.EnableHighlight(Color.blue);
                    if (searchToCell)
                        hexGrid.FindPath(searchFromCell, searchToCell, 24);
                }
            }
            else if (searchFromCell && searchFromCell != currentCell)
            {
                if (searchToCell != currentCell)
                {
                    searchToCell = currentCell;
                    hexGrid.FindPath(searchFromCell, currentCell, 24);
                }
            }
            //else
            //    hexGrid.FindDistancesTo(currentCell);

            previousCell = currentCell;
        }
        else
        {
            previousCell = null;
        }
    }

    private void EditCells(HexCell center)
    {
        int centerX = center.Coordinates.X;
        int centerZ = center.Coordinates.Z;

        for(int r = 0, z = centerZ - myBrushSize; z <= centerZ; z++, r++)
        {
            for(int x = centerX - r; x <= centerX + myBrushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for(int r = 0, z = centerZ + myBrushSize; z > centerZ; z--, r++)
        {
            for(int x = centerX - myBrushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell aCell)
    {
        if (aCell)
        {
            if(myActiveTerrainTypeIndex >= 0)
                aCell.TerrainTypeIndex = myActiveTerrainTypeIndex;

            if (myApplyElevation)
                aCell.Elevation = myActiveElevation;

            if (myApplyWaterLevel)
                aCell.WaterLevel = myActiveWaterLevel;

            if (myApplySpecialIndex)
                aCell.SpecialIndex = myActiveSpecialIndex;

            if (myApplyUrbanLevel)
                aCell.UrbanLevel = myActiveUrbanLevel;

            if (myApplyFarmLevel)
                aCell.FarmLevel = myActiveFarmLevel;

            if (myApplyPlantLevel)
                aCell.PlantLevel = myActivePlantLevel;

            if (myRiverMode == OptionalToggle.NO)
                aCell.RemoveRiver();
            
            if (myRoadMode == OptionalToggle.NO)
                aCell.RemoveRoads();

            if (myWalledMode != OptionalToggle.IGNORE)
                aCell.Walled = myWalledMode == OptionalToggle.YES;
            
            if (isDrag)
            {
                HexCell otherCell = aCell.GetNeighbor(dragDirection.Opposite());
                if (otherCell)
                {
                    if(myRiverMode == OptionalToggle.YES)
                        otherCell.SetOutgoingRiver(dragDirection);
                    if (myRoadMode == OptionalToggle.YES)
                        otherCell.AddRoad(dragDirection);
                }
            }
        }
    }

    private void CreateUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit)
        {
            //HexUnit unit = Instantiate(unitPrefab);
            //unit.transform.SetParent(hexGrid.transform, false);
            //unit.Location = cell;
            //unit.Orientation = Random.Range(0f, 360f);
            hexGrid.AddUnit(Instantiate(unitPrefab), cell, Random.Range(0f, 360f));
        }
    }

    private void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if(cell && cell.Unit)
        {
            cell.Unit.Die();
        }
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for(dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
        {
            if(previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }

    private HexCell GetCellUnderCursor()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(inputRay, out hit))
        {
            return hexGrid.GetCell(hit.point);
        }
        return null;
    }

    public void SetTerrainTypeIndex(int index)
    {
        myActiveTerrainTypeIndex = index;
    }

    public void SetApplyElevation(bool toggle)
    {
        myApplyElevation = toggle;
    }

    public void SetElevation(float anElevation)
    {
        myActiveElevation = (int)anElevation;
    }

    public void SetApplyWaterLevel(bool toggle)
    {
        myApplyWaterLevel = toggle;
    }

    public void SetWaterLevel(float aLevel)
    {
        myActiveWaterLevel = (int)aLevel;
    }

    public void SetApplyUrbanLevel(bool toggle)
    {
        myApplyUrbanLevel = toggle;
    }
    
    public void SetUrbanLevel(float aLevel)
    {
        myActiveUrbanLevel = (int)aLevel;
    }

    public void SetApplyFarmLevel(bool toggle)
    {
        myApplyFarmLevel = toggle;
    }

    public void SetFarmLevel(float aLevel)
    {
        myActiveFarmLevel = (int)aLevel;
    }

    public void SetApplyPlantLevel(bool toggle)
    {
        myApplyPlantLevel = toggle;
    }

    public void SetApplySpecialIndex(bool toggle)
    {
        myApplySpecialIndex = toggle;
    }

    public void SetPlantLevel(float aLevel)
    {
        myActivePlantLevel = (int)aLevel;
    }

    public void SetBrushSize(float aSize)
    {
        myBrushSize = (int)aSize;
    }

    public void SetRiverMode(int aMode)
    {
        myRiverMode = (OptionalToggle)aMode;
    }

    public void SetRoadMode(int aMode)
    {
        myRoadMode = (OptionalToggle)aMode;
    }

    public void SetWalledMode(int aMode)
    {
        myWalledMode = (OptionalToggle)aMode;
    }

    public void SetSpecialIndex(float index)
    {
        myActiveSpecialIndex = (int)index;
    }

    public void Save() 
    {
        string path = Path.Combine(Application.persistentDataPath, "testtest.map");
        using (BinaryWriter writter = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writter.Write(1);
            hexGrid.Save(writter);
        }
    }

    public void Load() 
    {
        string path = Path.Combine(Application.persistentDataPath, "test.map");
        using(BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int header = reader.ReadInt32();
            if (header <= 1)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else
                Debug.LogWarning("Unknown map format " + header);
        }
    }

    public void ShowGrid(bool visible)
    {
        if(visible)
        {
            terrainMaterial.EnableKeyword("GRID_ON");
        }
        else
        {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    public void SetEditMode(bool toggle)
    {
        editMode = toggle;
        hexGrid.ShowUI(!toggle);
    }
}
