using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Backends.HexGrid;


public class HexGrid : MonoBehaviour
{
    public int Width = 6;
    public int Height = 6;

    public  HexCell CellPrefab;
    public Text CellLabelPrefab;

    private HexCell[] myCells;
    private HexMesh myHexMesh;

    private Canvas myGridCanvas;

    void Awake()
    {
        myHexMesh = GetComponentInChildren<HexMesh>();
        myGridCanvas = GetComponentInChildren<Canvas>();


        myCells = new HexCell[Height * Width];

        for(int z = 0, i = 0; z < Height; z++)
        {
            for(int x = 0; x < Width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void Start()
    {
        myHexMesh.Triangulate(myCells);
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

        Text label = Instantiate<Text>(CellLabelPrefab);
        label.rectTransform.SetParent(myGridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        //label.text = "x: " + x.ToString() + "\n" + "z: " + z.ToString();
        label.text = cell.Coordinates.ToStringOnSeperateLines();
    }
}
