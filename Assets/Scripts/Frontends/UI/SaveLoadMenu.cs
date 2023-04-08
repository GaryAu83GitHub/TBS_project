using UnityEngine;

public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid hexGrid;

    private bool saveMode;

    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;

        gameObject.SetActive(true);
        HexMapCamera.Looked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Looked = false;
    }
}
