using UnityEngine;

namespace Assets.Scripts.Backends.HexGrid
{
    [System.Serializable]
    public struct HexCoordinates
    {
        public static HexCoordinates FromOffsetCoordinates(int x, int z)
        {
            return new HexCoordinates(x - z / 2, z);
        }

        public static HexCoordinates FromPosition(Vector3 position)
        {            
            float x = position.x / (HexMetrics.InnerRadius * 2f);
            float y = -x;

            float offset = position.z / (HexMetrics.OuterRadius * 3f);
            x -= offset;
            y -= offset;

            int iX = Mathf.RoundToInt(x);
            int iY = Mathf.RoundToInt(y);
            int iZ = Mathf.RoundToInt(-x - y);

            if (iX + iY + iZ != 0)
            {
                float dX = Mathf.Abs(x - iX);
                float dY = Mathf.Abs(y - iY);
                float dZ = Mathf.Abs(-x - y - iZ);

                if (dX > dY && dX > dZ)
                    iX = -iY - iZ;
                else if (dZ > dY)
                    iZ = -iX - iY;
            }

            return new HexCoordinates(iX, iZ);
        }

        [SerializeField]
        private int x, z;

        public int X { get { return x; } }
        public int Y { get { return -X - Z; } }
        public int Z { get { return z; } }

        public HexCoordinates(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        public override string ToString()
        {
            return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
        }

        public string ToStringOnSeperateLines() 
        {
            return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
        }

        public int DistanceTo(HexCoordinates other)
        {
            return (
                (x < other.x ? other.x - x : x - other.x) +
                (Y < other.Y ? other.Y - Y : Y - other.Y) +
                (z < other.z ? other.z - z : z - other.z)) / 2;
        }
    }
}
