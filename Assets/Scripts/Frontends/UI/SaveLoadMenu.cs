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

    public void Action()
    {
        string path = GetSelectedPath();
        if (path == null)
            return;

        if(saveMode)
            Save(path);
        else
            Load(path);

        Close();
    }

    public void SelectItem(string name)
    {
        nameInput.text = name;
    }

    private string GetSelectedPath()
    {
        string mapName = nameInput.text;
        if (mapName.Length == 0)
            return null;

        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    private void Save(string path)
    {
        using (BinaryWriter writter = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writter.Write(1);
            hexGrid.Save(writter);
        }
    }

    private void Load(string path)
    {
        if(!File.Exists(path))
        {
            Debug.LogError("File does not exist " + path);
            return;
        }

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int header = reader.ReadInt32();
            if (header <= 1)
            {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else
            {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }
}
