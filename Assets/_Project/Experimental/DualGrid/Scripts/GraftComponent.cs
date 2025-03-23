using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityUtils;

public class GraftComponent : MonoBehaviour
{
    [field: ReadOnly] public GraftCell GraftCell;

    public void Init(GraftCell graftCell, GameObject meshPrefab, float cellSize)
    {
        UpdateGraft(graftCell, meshPrefab, cellSize);
    }

    public void UpdateGraft(GraftCell graftCell, GameObject meshPrefab, float cellSize)
    {
        GraftCell = graftCell;

        transform.Children().ForEach(trans => Destroy(trans.gameObject));

#if UNITY_EDITOR
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(meshPrefab, transform);
#else
        GameObject instance = Instantiate(meshPrefab, GraftCell.Position, Quaternion.Euler(0, GraftCell.Rotation, 0));
        instance.transform.SetParent = transform;
#endif

        instance.transform.position = GraftCell.position;
        instance.transform.localScale = Vector3.one * cellSize;
        instance.transform.rotation = Quaternion.Euler(0, graftCell.rotation, 0);

    }

}