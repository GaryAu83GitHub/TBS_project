using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour
{
    public Text menuLabel, actionButtonLabel;

    public InputField nameInput;

    public HexGrid hexGrid;

    private bool saveMode;

    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;
        if(saveMode)
        {
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else
        {
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }

        gameObject.SetActive(true);
        HexMapCamera.Looked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Looked = false;
    }

    private string GetSelectedPath()
    {
        string mapName = nameInput.text;
        if (mapName.Length == 0)
            return null;

        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }
}
