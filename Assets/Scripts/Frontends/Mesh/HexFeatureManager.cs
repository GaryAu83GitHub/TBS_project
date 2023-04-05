using UnityEngine;
using Assets.Scripts.Backends.HexGrid;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private HexFeatureCollection[] urbanCollections;

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
        Transform prefab = PickPrefab(aCell.UrbanLevel, hash.A, hash.B);
        if (!prefab)
            return;

        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * .5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360 * hash.C, 0f);
        instance.SetParent(container, false);
    }

    private Transform PickPrefab(int aLevel, float aHash, float choice)
    {
        if(aLevel > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(aLevel - 1);
            for(int i = 0; i < thresholds.Length; i++)
            {
                if(aHash < thresholds[i])
                {
                    return urbanCollections[i].Pick(choice);
                }
            }
        }
        return null;
    }
}
