using UnityEngine;
using Assets.Scripts.Backends.HexGrid;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;

    [SerializeField]
    private HexMesh walls;

    private Transform container;

    public void Clear() 
    {
        if (container)
            Destroy(container.gameObject);

        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);

        walls.Clear();
    }
    public void Apply() 
    {
        walls.Apply();
    }

    public void AddFeature(HexCell aCell, Vector3 position) 
    {
        HexHash hash = HexMetrics.SampleHashGrid(position);
        //if (hash.A >= aCell.UrbanLevel * .25f)
        //    return;
        Transform prefab = PickPrefab(urbanCollections, aCell.UrbanLevel, hash.A, hash.D);
        Transform otherPrefab = PickPrefab(farmCollections, aCell.FarmLevel, hash.B, hash.D);

        float usedHash = hash.A;
        if (prefab)
        {
            if (otherPrefab && hash.B < hash.A)
            {
                prefab = otherPrefab;
                usedHash = hash.B;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
            usedHash = hash.B;
        }

        otherPrefab = PickPrefab(plantCollections, aCell.PlantLevel, hash.C, hash.D);
        if (prefab)
        {
            if (otherPrefab && hash.C < usedHash)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
            prefab = otherPrefab;
        else
            return;

        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * .5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360 * hash.E, 0f);
        instance.SetParent(container, false);
    }

    public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell)
    {
        if(nearCell.Walled != farCell.Walled)
        {
            AddWallSegment(near.v1, far.v1, near.v5, far.v5);
        }
    }

    private Transform PickPrefab(HexFeatureCollection[] collection, int aLevel, float aHash, float choice)
    {
        if(aLevel > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(aLevel - 1);
            for(int i = 0; i < thresholds.Length; i++)
            {
                if(aHash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }
        return null;
    }

    private void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight)
    {
        Vector3 left = Vector3.Lerp(nearLeft, farLeft, .5f);
        Vector3 right = Vector3.Lerp(nearRight, farRight, .5f);

        Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

        Vector3 v1, v2, v3, v4;

        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.y = v4.y = left.y + HexMetrics.WallHeight;
        walls.AddQuad(v1, v2, v3, v4);

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = v4.y = left.y + HexMetrics.WallHeight;
        walls.AddQuad(v2, v1, v4, v3);
    }
}
