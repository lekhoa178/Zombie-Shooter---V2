using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityUtils;

public class MapSubsystem : Singleton<MapSubsystem>
{
    public int N = 5; // Horizontal range (-N to N)
    public int M = 5; // Vertical range (-M to M)
    public float cellSize = 1.0f; // Size of each grid cell

    public GameObject[] graftPrefabs; // Array of prefabs for each graft type

    private DualGraphMap mapData;

    private TileComponent[,] tileComponentGrid;
    private GraftComponent[,] graftComponentGrid;

    public Action<float> OnGridGenerated;

    private void Start()
    {
        Initialize();

        mapData = DualGraphGenerator.GenerateMap(N, M, cellSize);
        SpawnMeshes(mapData);
    }

    private void Initialize()
    {
        tileComponentGrid = new TileComponent[2 * N + 1, 2 * M + 1];
        graftComponentGrid = new GraftComponent[2 * N + 2, 2 * M + 2];
    }

    private void SpawnMeshes(DualGraphMap mapData)
    {
        TileCell[,] TileGrid = mapData.TileGrid;
        GraftCell[,] GraftGrid = mapData.GraftGrid;

        GameObject TileGameObject = new GameObject("Tile Grid");
        TileGameObject.transform.SetParent(transform);

        GameObject GraftGameObject = new GameObject("Graft Grid");
        GraftGameObject.transform.SetParent(transform);

        foreach (var cell in GraftGrid)
        {
            if (cell.graftType < 0 || cell.graftType >= graftPrefabs.Length)
                continue;

            GameObject prefab = graftPrefabs[cell.graftType];

            GameObject instance = new GameObject($"Graft_{cell.x}_{cell.y}");
            instance.transform.SetParent(GraftGameObject.transform);
            GraftComponent graftComponent = instance.AddComponent<GraftComponent>();
            graftComponent.Init(cell, prefab, cellSize);
            graftComponentGrid[cell.x + N, cell.y + M] = graftComponent;

        }

        foreach (var cell in TileGrid)
        {
            GameObject instance = new GameObject($"Tile_{cell.x}_{cell.y}");
            instance.transform.SetParent(TileGameObject.transform);
            instance.transform.position = cell.position;

            TileComponent tc = instance.AddComponent<TileComponent>();
            tc.Init(cell, cellSize, UpdateCell);
            tileComponentGrid[cell.x + N, cell.y + M] = tc;
        }

        OnGridGenerated?.Invoke(cellSize);
    }

    public void UpdateCell(TileCell cell)
    {
        DualGraphGenerator.UpdateMap(mapData, new List<TileCell> { cell });
        GraftCell[] grafts = GetGraftCellsOfTile(cell);
        grafts.ForEach(gc => 
        {
            GameObject prefab = graftPrefabs[gc.graftType];

            graftComponentGrid[gc.x + N, gc.y + M].UpdateGraft(gc, prefab, cellSize);
        });
    }

    public GraftCell[] GetGraftCellsOfTile(TileCell tileCell)
    {
        return mapData.GetGraftCellsOfTile(tileCell);
    }

    public TileCell GetTileCellAt(int x, int y)
    {
        return mapData.GetTileCellAt(x, y);
    }

    public GraftCell GetGraftCellAt(int x, int y)
    {
        return mapData.GetGraftCellAt(x + N, y + M);
    }


    private void OnDrawGizmos()
    {
        TileCell[,] TileGrid = mapData.TileGrid;
        GraftCell[,] GraftGrid = mapData.GraftGrid;

        if (TileGrid != null)
        {
            foreach (var cell in TileGrid)
            {
                Gizmos.color = Color.green;
                DrawSquare(cell.position);
                Gizmos.color = cell.type == 0 ? Color.black : Color.white;
                Gizmos.DrawSphere(cell.position, 0.2f);
#if UNITY_EDITOR
                Handles.Label(cell.position + Vector3.up * 0.1f, $"VS: {cell.x}, {cell.y}°");
#endif
            }
        }

        if (GraftGrid != null)
        {
            Gizmos.color = Color.red;
            foreach (var cell in GraftGrid)
            {
                DrawSquare(cell.position);
#if UNITY_EDITOR
                Handles.Label(cell.position + Vector3.up * 0.1f, $"STR: {cell.x}, {cell.y}, {cell.graftType}°");
#endif
            }
        }
    }

    private void DrawSquare(Vector3 position)
    {
        Vector3 bottomLeft = position + new Vector3(-cellSize / 2, 0, -cellSize / 2);
        Vector3 bottomRight = position + new Vector3(cellSize / 2, 0, -cellSize / 2);
        Vector3 topRight = position + new Vector3(cellSize / 2, 0, cellSize / 2);
        Vector3 topLeft = position + new Vector3(-cellSize / 2, 0, cellSize / 2);

        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }
}