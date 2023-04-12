using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
    public HexGrid grid;

    private HexCell currentCell;
    private HexUnit selectedUnit;

    void Update()
    {
        if(!EventSystem.current.IsPointerOverGameObject())
        {
            if(Input.GetMouseButtonDown(0))
                DoSelection();
        }
    }

    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        grid.ShowUI(!toggle);
    }

    private bool UpdateCurrentCell()
    {
        HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));

        if(cell != currentCell)
        {
            currentCell = cell;
            return true;
        }
        return false;
    }

    private void DoSelection()
    {
        UpdateCurrentCell();

        if(currentCell)
        {
            selectedUnit = currentCell.Unit;
        }
    }
}
