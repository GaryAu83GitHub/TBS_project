using UnityEngine;
using Assets.Scripts.Backends.HexGrid;

public class HexFeatureManager : MonoBehaviour
{
    [SerializeField]
    private Transform featurePrefab;
    public void Clear() { }
    public void Apply() { }
    public void AddFeature(Vector3 position) 
    {
        Transform instance = Instantiate(featurePrefab);
        position.y += instance.localScale.y * .5f;
        instance.localPosition = HexMetrics.Perturb(position);
    }
}
