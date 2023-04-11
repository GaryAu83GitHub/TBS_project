using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public HexCell Location
    {
        get { return location; }
        set
        { 
            location = value;
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
}