using UnityEngine;
using Assets.Scripts.Backends.HexGrid.Tools;

namespace Assets.Scripts.Backends.HexGrid
{
    public enum HexEdgeType { FLAT, SLOPE, CLIFF }
    public static class HexMetrics
    {
        public static Texture2D NoiseSource;

        public const float OuterToInner = 0.866025404f;

        public const float InnerToOuter = 1f / OuterToInner;

        public const float OuterRadius = 10f;

        public const float InnerRadius = OuterRadius * OuterToInner;

        public const float SolidFactor = .8f;

        public const float BlendFactor = 1f - SolidFactor;

        public const float ElevationStep = 3f;

        public const float ElevationPerturbStrength = 1.5f;

        public const float StreamBedElevationOffset = -1.75f;

        public const float WaterElevationOffset = -.5f;

        public const float WaterFactor = .6f;

        public const float WaterBlendFactor = 1f - WaterFactor;

        public const int TerracesPerSlope = 2;

        public const int TerraceSteps = TerracesPerSlope * 2 + 1;

        public const float HorizontalTerraceStepSize = 1f / TerraceSteps;

        public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

        public const float CellPerturbStrength = 4f;

        public const float NoiseScale = .003f;

        public const int ChunkSizeX = 5, ChunkSizeZ = 5;

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

        public static Vector3 GetFirstSolidCorner(HexDirection aDir)
        {
            return Corners[(int)aDir] * SolidFactor;
        }

        public static Vector3 GetFirstWaterCorner(HexDirection aDir)
        {
            return Corners[(int)aDir] * WaterFactor;
        }

        public static Vector3 GetSecondCorner(HexDirection aDir)
        {
            return Corners[(int)aDir + 1];
        }

        public static Vector3 GetSecondSolidCorner(HexDirection aDir)
        {
            return Corners[(int)aDir + 1] * SolidFactor;
        }
        
        public static Vector3 GetSecondWaterCorner(HexDirection aDir)
        {
            return Corners[(int)aDir + 1] * WaterFactor;
        }

        public static Vector3 GetSolidEdgeMiddle(HexDirection aDir)
        {
            return (Corners[(int)aDir] + Corners[(int)aDir + 1]) * (.5f * SolidFactor);
        }

        public static Vector3 GetBridge(HexDirection aDir)
        {
            return (Corners[(int)aDir] + Corners[(int)aDir + 1]) * BlendFactor;
        }

        public static Vector3 GetWaterBridge(HexDirection aDir)
        {
            return (Corners[(int)aDir] + Corners[(int)aDir + 1]) * WaterBlendFactor;
        }

        public static Vector3 TerraceLerp(Vector3 aA, Vector3 aB, int aStep)
        {
            float h = aStep * HexMetrics.HorizontalTerraceStepSize;
            aA.x += (aB.x - aA.x) * h;
            aA.z += (aB.z - aA.z) * h;

            float v = ((aStep + 1) / 2) * HexMetrics.VerticalTerraceStepSize;
            aA.y += (aB.y - aA.y) * v;

            return aA;
        }

        public static Color TerraceLerp(Color aColorA, Color aColorB, int aStep)
        {
            float h = aStep * HexMetrics.HorizontalTerraceStepSize;
            return Color.Lerp(aColorA, aColorB, h);
        }

        public static HexEdgeType GetEdgeType(int anElevation1, int anElevation2)
        {
            if (anElevation1 == anElevation2)
                return HexEdgeType.FLAT;

            int delta = anElevation2 - anElevation1;
            if(delta == 1 || delta == -1)
                return HexEdgeType.SLOPE;

            return HexEdgeType.CLIFF;
        }

        public static Vector4 SampleNoise(Vector3 worldPosition) 
        {
            return NoiseSource.GetPixelBilinear(worldPosition.x * NoiseScale, worldPosition.z * NoiseScale);
        }

        public static Vector3 Perturb(Vector3 aPosition)
        {
            Vector4 sample = SampleNoise(aPosition);

            aPosition.x += (sample.x * 2f - 1f) * CellPerturbStrength;
            aPosition.z += (sample.z * 2f - 1f) * CellPerturbStrength;

            return aPosition;
        }
    }
}
