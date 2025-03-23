using System;
using Unity.Collections;
using UnityEngine;

public class TileComponent : MonoBehaviour
{
    [field: ReadOnly] public TileCell TileData;

    public Action<TileCell> OnTypeChanged { get; set; }

    public void Init(TileCell tileCell, float cellSize, Action<TileCell> inOnTypeChanged = null)
    {
        TileData = tileCell;
        OnTypeChanged += inOnTypeChanged;

        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(cellSize, 0.1f, cellSize);
        boxCollider.center = new Vector3(0, 0.05f, 0);
        boxCollider.isTrigger = true;
        int groundLayerValue = Mathf.RoundToInt(Mathf.Log(LayerUtils.GroundLayer.value, 2));
        boxCollider.gameObject.layer = groundLayerValue;
    }

    public void SetType(TileType type)
    {
        TileData.type = type;
        OnTypeChanged?.Invoke(TileData);
    }

}