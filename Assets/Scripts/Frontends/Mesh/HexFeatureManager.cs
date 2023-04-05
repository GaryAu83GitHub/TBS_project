using UnityEngine;
using Assets.Scripts.Backends.HexGrid;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private Transform[] urbanPrefabs;
    //private Transform featurePrefab;

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
        if (hash.A >= aCell.UrbanLevel * .25f)
            return;

        Transform instance = Instantiate(urbanPrefabs[aCell.UrbanLevel - 1]);
        position.y += instance.localScale.y * .5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360 * hash.B, 0f);
        instance.SetParent(container, false);
    }
}
