using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Assets.Scripts.Backends.HexGrid;
using Assets.Scripts.Backends.HexGrid.Tools;

public class HexMapEditor : MonoBehaviour
{
    private enum OptionalToggle { IGNORE, YES, NO }

    [SerializeField]
    public Color[] Colors;

    [SerializeField]
    public HexGrid HexGrid;

    private Color myActiveColor;
    private int myActiveElevation;

    private bool myApplyColor;
    private bool myApplyElevation = true;

    private int myBrushSize;

    private OptionalToggle myRiverMode;

    private bool myIsDrag;
    private HexDirection myDragDirection;
    private HexCell myPreviousCell;

    void Awake()
    {
        SelectColor(0);
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            HandleInput();
        else
        {
            myPreviousCell = null;
        }
    }

    private void HandleInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            HexCell currentCell = HexGrid.GetCell(hit.point);
            if (myPreviousCell && myPreviousCell != currentCell)
                ValidateDrag(currentCell);
            else
                myIsDrag = false;

            EditCells(currentCell);
            myPreviousCell = currentCell;
        }
        else
        {
            myPreviousCell = null;
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
                EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for(int r = 0, z = centerZ + myBrushSize; z > centerZ; z--, r++)
        {
            for(int x = centerX - myBrushSize; x <= centerX + r; x++)
            {
                EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell aCell)
    {
        if (aCell)
        {
            if (myApplyColor)
                aCell.Color = myActiveColor;

            if (myApplyElevation)
                aCell.Elevation = myActiveElevation;

            if (myRiverMode == OptionalToggle.NO)
                aCell.RemoveRiver();
            else if (myIsDrag && myRiverMode == OptionalToggle.YES)
            {
                HexCell otherCell = aCell.GetNeighbor(myDragDirection.Opposite());
                if(otherCell)
                    otherCell.SetOutgoingRiver(myDragDirection);
            }
        }
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for(myDragDirection = HexDirection.NE; myDragDirection <= HexDirection.NW; myDragDirection++)
        {
            if(myPreviousCell.GetNeighbor(myDragDirection) == currentCell)
            {
                myIsDrag = true;
                return;
            }
        }
        myIsDrag = false;
    }

    public void SelectColor(int anIndex)
    {
        myApplyColor = anIndex >= 0;

        if(myApplyColor)
            myActiveColor = Colors[anIndex];
    }

    public void SetElevation(float anElevation)
    {
        myActiveElevation = (int)anElevation;
    }

    public void SetBrushSize(float aSize)
    {
        myBrushSize = (int)aSize;
    }

    public void SetApplyElevation(bool toggle)
    {
        myApplyElevation = toggle;
    }

    public void ShowUI(bool visible)
    {
        HexGrid.ShowUI(visible);
    }

    public void SetRiverMode(int aMode)
    {
        myRiverMode = (OptionalToggle)aMode;
    }
}
