using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] Colors;

    public HexGrid HexGrid;

    private Color myActiveColor;
    private int myActiveElevation;

    void Awake()
    {
        SelectColor(0);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            HandleInput();
    }

    private void HandleInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
            EditCell(HexGrid.GetCell(hit.point));
    }

    private void EditCell(HexCell aCell)
    {
        aCell.Color = myActiveColor;
        aCell.Elevation = myActiveElevation;
    }

    public void SelectColor(int anIndex)
    {
        myActiveColor = Colors[anIndex];
    }

    public void SetElevation(float anElevation)
    {
        myActiveElevation = (int)anElevation;
    }
}
