using UnityEngine;

namespace Assets.Scripts.Backends.HexGrid
{
    [System.Serializable]
    public struct HexFeatureCollection
    {
        public Transform[] Prefabs;

        public Transform Pick(float aChoice)
        {
            return Prefabs[(int)(aChoice * Prefabs.Length)];
        }
    }
}
