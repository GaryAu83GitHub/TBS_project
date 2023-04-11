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
}
