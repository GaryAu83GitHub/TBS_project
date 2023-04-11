using UnityEngine;
using System.IO;

public class HexUnit : MonoBehaviour
{
    public HexCell Location
    {
        get { return location; }
        set
        { 
            location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }
    private HexCell location;

    public float Orientation
    {
        get { return orientation; }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }
    private float orientation;

    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    public void Die()
    {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        location.Coordinates.Save(writer);
        writer.Write(orientation);
    }
}
