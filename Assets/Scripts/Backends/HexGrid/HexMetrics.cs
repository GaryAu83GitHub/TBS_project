using UnityEngine;
using Assets.Scripts.Backends.HexGrid.Tools;

namespace Assets.Scripts.Backends.HexGrid
{
    public static class HexMetrics
    {
        public const float OuterRadius = 10f;

        public const float InnerRadius = OuterRadius * 0.866025404f;

        static Vector3[] Corners = { 
            new Vector3 (0f, 0f, OuterRadius),
            new Vector3 (InnerRadius, 0f, .5f * OuterRadius),
            new Vector3 (InnerRadius, 0f, -.5f * OuterRadius),
            new Vector3 (0f, 0f, -OuterRadius),
            new Vector3 (-InnerRadius, 0f, -.5f * OuterRadius),
            new Vector3 (-InnerRadius, 0f, .5f * OuterRadius),
            new Vector3 (0f, 0f, OuterRadius)
        };

        public static Vector3 GetFirstCorner(HexDirection aDir)
        {
            return Corners[(int)aDir];
        }

        public static Vector3 GetSecondCorner(HexDirection aDir)
        {
            return Corners[(int)aDir + 1];
        }

    }
}
