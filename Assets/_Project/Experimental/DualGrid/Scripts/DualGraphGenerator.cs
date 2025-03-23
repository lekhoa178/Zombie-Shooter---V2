using System;
using System.Collections.Generic;
using UnityEngine;

static public class DualGraphGenerator
{
    static public DualGraphMap GenerateMap(int N, int M, float cellSize)
    {
        DualGraphMap data = new DualGraphMap();
        data.N = N;
        data.M = M;
        data.CellSize = cellSize;
        data.TileGrid = GenerateTileGrid(N, M, cellSize);
        data.GraftGrid = GenerateGraftGrid(N, M, cellSize);

        DetermineGraftTypes(data);
        return data;
    }

    static public void UpdateMap(DualGraphMap mapData, List<TileCell> updatedTileCells)
    {
        TileCell[,] TileGrid = mapData.TileGrid;
        GraftCell[,] GraftGrid = mapData.GraftGrid;

        // Track affected Graft grid cells
        HashSet<(int, int)> affectedGraftCells = new HashSet<(int, int)>();

        foreach (var cell in updatedTileCells)
        {
            // Update the Tile grid cell
            TileGrid[cell.x + mapData.N, cell.y + mapData.M] = cell;

            // Determine which Graft grid cells may be affected
            // Graft cells are shifted by 0.5, so we consider neighbors
            int GraftX = cell.x + mapData.N;
            int GraftY = cell.y + mapData.M;

            affectedGraftCells.Add((GraftX, GraftY));
            affectedGraftCells.Add((GraftX + 1, GraftY));
            affectedGraftCells.Add((GraftX, GraftY + 1));
            affectedGraftCells.Add((GraftX + 1, GraftY + 1));
        }

        // Update affected Graft grid cells
        foreach (var (x, y) in affectedGraftCells)
        {
            if (x < 0 || y < 0 || x >= GraftGrid.GetLength(0) || y >= GraftGrid.GetLength(1))
                continue;

            int[] types = new int[4];
            types[0] = (x > 0 && y > 0) ? ((int)TileGrid[x - 1, y - 1].type) : 0; // Bottom-left
            types[1] = (x < TileGrid.GetLength(0) && y > 0) ? ((int)TileGrid[x, y - 1].type) : 0; // Bottom-right
            types[2] = (x < TileGrid.GetLength(0) && y < TileGrid.GetLength(1)) ? ((int)TileGrid[x, y].type) : 0; // Top-right
            types[3] = (x > 0 && y < TileGrid.GetLength(1)) ? ((int)TileGrid[x - 1, y].type) : 0; // Top-left

            // Determine graft type and rotation
            (int graftType, int rotation) = GetGraftTypeAndRotation(types);

            // Update Graft grid
            GraftGrid[x, y].graftType = graftType;
            GraftGrid[x, y].rotation = rotation;
        }

    }


    static private TileCell[,] GenerateTileGrid(int N, int M, float cellSize)
    {
        int width = 2 * N + 1;
        int height = 2 * M + 1;

        TileCell[,] TileGrid = new TileCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float worldX = (x - N) * cellSize;
                float worldZ = (y - M) * cellSize;

                TileGrid[x, y] = new TileCell
                {
                    x = x - N,
                    y = y - M,
                    type = (TileType)1,
                    position = new Vector3(worldX, 0, worldZ)
                };
            }
        }

        return TileGrid;
    }

    static private GraftCell[,] GenerateGraftGrid(int N, int M, float cellSize)
    {
        int width = 2 * N + 2;
        int height = 2 * M + 2;

        GraftCell[,] GraftGrid = new GraftCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float worldX = (x - N - 0.5f) * cellSize;
                float worldZ = (y - M - 0.5f) * cellSize;

                GraftGrid[x, y] = new GraftCell
                {
                    x = x - N,
                    y = y - M,
                    position = new Vector3(worldX, 0, worldZ),
                    graftType = -1,
                    rotation = 0
                };
            }
        }

        return GraftGrid;
    }

    static private void DetermineGraftTypes(DualGraphMap mapData)
    {
        TileCell[,] TileGrid = mapData.TileGrid;
        GraftCell[,] GraftGrid = mapData.GraftGrid;

        int width = GraftGrid.GetLength(0);
        int height = GraftGrid.GetLength(1);

        // Traverse through each Graft grid cell
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int vX = x;
                int vY = y;

                int[] types = new int[4] { 0, 0, 0, 0 };

                // Bottom-Left
                if (vX > 0 && vY > 0)
                {
                    types[0] = ((int)TileGrid[vX - 1, vY - 1].type);
                }

                // Bottom-Right
                if (vX < TileGrid.GetLength(0) && vY > 0)
                {
                    types[1] = ((int)TileGrid[vX, vY - 1].type);
                }

                // Top-Right
                if (vX < TileGrid.GetLength(0) && vY < TileGrid.GetLength(1))
                {
                    types[2] = ((int)TileGrid[vX, vY].type);
                }

                // Top-Left
                if (vX > 0 && vY < TileGrid.GetLength(1))
                {
                    types[3] = ((int)TileGrid[vX - 1, vY].type);
                }

                //types[0] = (vX > 0 && vY > 0) ? TileGrid[vX - 1, vY - 1].type : 0; // Bottom-left
                //types[1] = (vX < TileGrid.GetLength(0) && vY > 0) ? TileGrid[vX, vY - 1].type : 0; // Bottom-right
                //types[2] = (vX < TileGrid.GetLength(0) && vY < TileGrid.GetLength(1)) ? TileGrid[vX, vY].type : 0; // Top-right
                //types[3] = (vX > 0 && vY < TileGrid.GetLength(1)) ? TileGrid[vX - 1, vY].type : 0; // Top-left

                (int graftType, int rotation) = GetGraftTypeAndRotation(types);

                GraftGrid[x, y].graftType = graftType;
                GraftGrid[x, y].rotation = rotation;
            }
        }
    }

    static private (int graftType, int rotation) GetGraftTypeAndRotation(int[] types)
    {
        string signature = string.Join("", types);

        Dictionary<string, (int graftType, int rotation)> patternMap = new Dictionary<string, (int, int)>
        {
            // Type 0: Full grass
            { "0000", (0, 0) },

            // Type 1: Full soil
            { "1111", (1, 0) },

            // Type 2: Three corners grass, one soil
            { "1000", (2, 0) },   // Bottom-left soil
            { "0100", (2, 270) },  // Bottom-right soil
            { "0010", (2, 180) }, // Top-right soil
            { "0001", (2, 90) }, // Top-left soil

            // Type 3: Three corners soil, one grass
            { "0111", (3, 0) },   // Bottom-left grass
            { "1011", (3, 270) },  // Bottom-right grass
            { "1101", (3, 180) }, // Top-right grass
            { "1110", (3, 90) }, // Top-left grass

            // Type 4: Two adjacent corners same, two opposite
            { "0011", (4, 0) },   // Bottom same
            { "1100", (4, 180) }, // Top same
            { "0110", (4, 90) },  // Left same
            { "1001", (4, 270) }, // Right same

            // Type 5: Two opposite corners same
            { "1010", (5, 0) },   // Diagonal
            { "0101", (5, 90) }   // Diagonal (rotated)
        };

        if (patternMap.ContainsKey(signature))
        {
            return patternMap[signature];
        }

        return (-1, 0);
    }
}

public struct DualGraphMap
{
    public int N;
    public int M;
    public float CellSize;

    public TileCell[,] TileGrid;
    public GraftCell[,] GraftGrid;

    public TileCell GetTileCellAt(int x, int y)
    {
        return TileGrid[x + N, y + M];
    }

    public GraftCell GetGraftCellAt(int x, int y)
    {
        return GraftGrid[x + N, y + M];
    }

    public GraftCell[] GetGraftCellsOfTile(TileCell tileCell)
    {
        List<GraftCell> graftCells = new List<GraftCell>();

        int x = tileCell.x + N;
        int y = tileCell.y + M;

        if (x >= 0 && x < GraftGrid.GetLength(0) && y >= 0 && y < GraftGrid.GetLength(1))
        {
            graftCells.Add(GraftGrid[x, y]);
        }

        if (x >= 0 && x < GraftGrid.GetLength(0) && y + 1 >= 0 && y + 1 < GraftGrid.GetLength(1))
        {
            graftCells.Add(GraftGrid[x, y + 1]);
        }

        if (x + 1 >= 0 && x + 1 < GraftGrid.GetLength(0) && y >= 0 && y < GraftGrid.GetLength(1))
        {
            graftCells.Add(GraftGrid[x + 1, y]);
        }

        if (x + 1 >= 0 && x + 1 < GraftGrid.GetLength(0) && y + 1 >= 0 && y + 1 < GraftGrid.GetLength(1))
        {
            graftCells.Add(GraftGrid[x + 1, y + 1]);
        }

        return graftCells.ToArray();
    }
}

[Serializable]
public struct TileCell
{
    public int x;
    public int y;
    public TileType type; // 0: grass, 1: soil
    public Vector3 position;
    public GameObject gameObject;
}

[Serializable]
public struct GraftCell
{
    public int x;
    public int y;
    public Vector3 position;
    public int graftType; // Type of graft
    public int rotation; // Rotation of graft in degrees
    public GameObject gameObject;
}

public enum ECornerType
{
    BottomLeft = 0,
    BottomRight = 1,
    TopRight = 2,
    TopLeft = 3
}

public enum TileType
{
    Grass = 0,
    Soil = 1
}