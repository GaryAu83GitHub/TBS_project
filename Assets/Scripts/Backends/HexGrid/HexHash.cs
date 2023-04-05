using UnityEngine;

namespace Assets.Scripts.Backends.HexGrid
{
    public struct HexHash
    {
        public float A, B;

        public static HexHash Create()
        {
            HexHash hash;
            hash.A = Random.value;
            hash.B = Random.value;

            return hash;
        }
    }
}
