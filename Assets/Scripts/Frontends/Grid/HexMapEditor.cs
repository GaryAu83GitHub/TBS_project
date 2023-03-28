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

    private bool myApplyColor;
    private bool myApplyElevation = true;

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
        if (myApplyColor)
            aCell.Color = myActiveColor; 

        if(myApplyElevation)
            aCell.Elevation = myActiveElevation;
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

    public void SetApplyElevation(bool toggle)
    {
        myApplyElevation = toggle;
    }
}
