using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Backends.HexGrid;

public class HexGridChunk : MonoBehaviour
{
    private HexCell[] myCells;
    private HexMesh myHexMesh;
    Canvas myGridCanvas;

    private void Awake()
    {
        myGridCanvas = GetComponentInChildren<Canvas>();
        myHexMesh = GetComponentInChildren<HexMesh>();

        myCells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        ShowUI(false);
    }

    //private void Start()
    //{
    //    myHexMesh.Triangulate(myCells);
    //}

    private void LateUpdate()
    {
        myHexMesh.Triangulate(myCells);
        enabled = false;
    }

    public void AddCell(int index, HexCell aCell)
    {
        myCells[index] = aCell;
        aCell.Chunk = this;
        aCell.transform.SetParent(transform, false);
        aCell.UIRect.SetParent(myGridCanvas.transform, false);
    }

    public void Refresh()
    {
        //myHexMesh.Triangulate(myCells);
        enabled = true;
    }

    public void ShowUI(bool visible)
    {
        myGridCanvas.gameObject.SetActive(visible);
    }
}
