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
    }

    private void Start()
    {
        myHexMesh.Triangulate(myCells);
    }

    public void AddCell(int index, HexCell aCell)
    {
        myCells[index] = aCell;
        aCell.transform.SetParent(transform, false);
        aCell.UIRect.SetParent(myGridCanvas.transform, false);
    }
}
