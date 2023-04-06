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
            AddWallSegment(near.v1, far.v1, near.v2, far.v2);
            AddWallSegment(near.v2, far.v2, near.v3, far.v3);
            AddWallSegment(near.v3, far.v3, near.v4, far.v4);
            AddWallSegment(near.v4, far.v4, near.v5, far.v5);
        }
    }

    public void AddWall(Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3)
    {
        if(cell1.Walled)
        {
            if(cell2.Walled)
            {
                if(!cell3.Walled)
                {
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if(cell3.Walled)
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if(cell2.Walled)
        {
            if(cell3.Walled)
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if(cell3.Walled)
        {
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
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

        float leftTop = left.y + HexMetrics.WallHeight;
        float rightTop = right.y + HexMetrics.WallHeight;

        Vector3 v1, v2, v3, v4;

        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop; ;
        walls.AddQuad(v1, v2, v3, v4);

        Vector3 t1 = v3, t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuad(v2, v1, v4, v3);

        walls.AddQuad(t1, t2, v3, v4);
    }

    private void AddWallSegment(Vector3 pivot, HexCell pivotCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
    {
        AddWallSegment(pivot, left, pivot, right);
    }
}
