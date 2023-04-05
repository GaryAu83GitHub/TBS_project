using UnityEngine;
using Assets.Scripts.Backends.HexGrid;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;

    private Transform container;

    public void Clear() 
    {
        if (container)
            Destroy(container.gameObject);

        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }
    public void Apply() { }
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
}
