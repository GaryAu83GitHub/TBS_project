using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int Width = 6;
    public int Height = 6;

    public  HexCell CellPrefab;
    public Text CellLabelPrefab;

    private HexCell[] myCells;
    private Canvas myGridCanvas;

    private void Awake()
    {
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

    private void CreateCell(int x, int z, int i)
    {
        Vector3 pos = new Vector3(x * 10f, 0f, z * 10f);

        HexCell cell = myCells[i] = Instantiate<HexCell>(CellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = pos;

        Text label = Instantiate<Text>(CellLabelPrefab);
        label.rectTransform.SetParent(myGridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(pos.x, pos.z);
        label.text = "x: " + x.ToString() + "\n" + "z: " + z.ToString();
    }
}
