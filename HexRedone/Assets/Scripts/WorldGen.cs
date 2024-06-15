﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Text;

[System.Serializable]
public class Ore
{
    public string name;
    public GameObject prefab;
}

public class ElementCounters
{
    private Dictionary<string, int> elementCounts;

    public ElementCounters()
    {
        elementCounts = new Dictionary<string, int>();
    }

    public void IncrementCount(string element)
    {
        if (string.IsNullOrEmpty(element))
        {
            // Handle invalid element name (optional)
            Debug.LogWarning("Invalid element name provided.");
            return;
        }

        element = element.ToLower(); // Ensure case-insensitive counting

        if (elementCounts.ContainsKey(element))
        {
            elementCounts[element]++;
        }
        else
        {
            elementCounts[element] = 1;
        }

        //Debug.Log("Element added/updated: " + element + " Current count: " + elementCounts[element]);
    }
}

public static class Elements
{
    public const string
        None = "none",
        Tree = "tree",
        Water = "water",
        Earth = "earth",
        CoalOre = "coal-ore",
        IronOre = "iron-ore",
        CopperOre = "copper-ore",
        GoldOre = "gold-ore",
        UraniumOre = "uranium-ore",
        Stone = "stone",
        Oil = "oil",
        NaturalGas = "natual-gas";
}

[System.Serializable]
public class TileData
{
    public int TileID { get; set; }
    public int x { get; set; }
    public int z { get; set; }
    public string resourceType { get; set; }

    public TileData() { }

    public TileData(int tileID, int x, int z, string resourceType)
    {
        this.TileID = tileID;
        this.x = x;
        this.z = z;
        this.resourceType = resourceType;
    }
}

public class WorldGen : MonoBehaviour
{
    public GameObject baseTilePrefab;
    ElementCounters elementCounter = new ElementCounters();

    [Range(0f, 1f)]
    [Header("Coal Ore Range")]
    public float coaloreRange;
    [Range(0f, 1f)]
    [Header("Iron Ore Range")]
    public float ironoreRange;
    [Range(0f, 1f)]
    [Header("Uranium Ore Range")]
    public float uraniumoreRange;
    [Range(0f, 1f)]
    [Header("Gold Ore Range")]
    public float goldoreRange;
    [Range(0f, 1f)]
    [Header("Water Range")]
    public float waterRange;
    [Range(0f, 1f)]
    [Header("Stone Range")]
    public float stoneRange;
    [Range(0f, 1f)]
    [Header("Earth Range")]
    public float earthRange;

    public int gridLength = 5;
    public int gridWidth = 5;

    public int tileWidth;
    public int tileHeight;

    public GameObject[,] tiles; // Declare a 2D array to store the tiles (BASE)
    public GameObject[,] resources; // 2D array to store the resource tiles (RESOURCES)
    public TileData[,] tileData;

    private Dictionary<int, TileData> tileDataDictionary = new Dictionary<int, TileData>();
    public List<Ore> ores;

    [Header("Perlin Noise Generator")]
    public double pHeight;
    public double pWidth;
    public float scaleFactor;
    public int octaveAmount;
    public float lacuAmount;
    public float persAmount;

    private Dictionary<string, Color> materialColors;

    private void InitializeMaterials()
    {
        materialColors = new Dictionary<string, Color>
        {
            { "water", new Color(0.0f, 0.0f, 1.0f) },          // Blue
            { "earth", new Color(0.545f, 0.271f, 0.075f) },    // Brown
            { "coal-ore", new Color(0.2f, 0.2f, 0.2f) },       // Dark Gray
            { "iron-ore", new Color(0.8f, 0.5f, 0.2f) },       // Iron Brown
            { "copper-ore", new Color(0.72f, 0.45f, 0.2f) },   // Copper Brown
            { "gold-ore", new Color(1.0f, 0.84f, 0.0f) },      // Gold
            { "uranium-ore", new Color(0.4f, 1.0f, 0.4f) },    // Light Green
            { "stone", new Color(0.5f, 0.5f, 0.5f) },          // Gray
            { "oil", new Color(0.1f, 0.1f, 0.1f) },            // Black
            { "natural-gas", new Color(0.68f, 0.85f, 0.9f) },  // Light Blue
            { "tree", new Color(0.13f, 0.55f, 0.13f) }         // Forest Green
        };
    }

    public float[,] GenerateNoiseMap(int width, int height, float scale, int seed, int octaves, float persistence, float lacunarity)
    {
        float[,] noiseMap = new float[width, height];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        return noiseMap;
    }

    void Start()
    {
        InitializeMaterials();
        RegenerateGrid();
    }

   public void InstantiateOre(string name, Vector3 position)
    {
        // Finds the Ore in the list with the specified name
        Ore oreToSpawn = ores.Find(ore => ore.name == name);

        if (oreToSpawn != null)
        {
            // Instantiate the prefab at the specified position
            Instantiate(oreToSpawn.prefab, position, Quaternion.identity);
        }
        else
        {
            Debug.Log("Ore not found in the list: " + name);
        }
    }

    public void SetTileResourceType(int tileID, string resourceType)
    {
        TileData data = GetTileData(tileID);

        if (data.x >= 0 && data.x < tileData.GetLength(0) && data.z >= 0 && data.z < tileData.GetLength(1))
        {
            Debug.Log("Setting resource type for tile at (" + data.x + ", " + data.z + ")");
            tileData[data.x, data.z].resourceType = resourceType;
            Debug.Log("New resource type is: " + tileData[data.x, data.z].resourceType);
        }
        else
        {
            Debug.Log("Cannot set resource type for tile at (" + data.x + ", " + data.z + ") - out of range");
        }
    }

    public int ExtractTileID(string tileName)
    {
        return int.Parse(tileName.Replace("tile", ""));
    }

    public TileData GetTileData(int tileID)
    {
        if (tileDataDictionary.TryGetValue(tileID, out TileData tileData))
        {
            return tileData;
        }
        return null;
    }

    public GameObject SetupTile(int x, int y, int tileID, string resourceType, float height)
    {
        double tileW = tileWidth * 1.732;
        double tileH = tileHeight * 1.5;

        double X = tileH * x;
        double Z = tileW * (y + 0.5 * (x % 2));

        GameObject baseTile = Instantiate(baseTilePrefab, new Vector3((float)X, height, (float)Z), Quaternion.identity);
        baseTile.name = "tile" + tileID;

        Renderer renderer = baseTile.GetComponent<Renderer>();
        if (renderer != null)
        {
            switch (resourceType)
            {
                case "water":
                    renderer.material.color = materialColors["water"];
                    elementCounter.IncrementCount(Elements.Water);
                    break;
                case "earth":
                    renderer.material.color = materialColors["earth"];
                    elementCounter.IncrementCount(Elements.Earth);
                    break;
                case "coal-ore":
                    renderer.material.color = materialColors["coal-ore"];
                    InstantiateOre("coal-ore", new Vector3((float)X, height + 2, (float)Z));
                    elementCounter.IncrementCount(Elements.CoalOre);
                    break;
                case "iron-ore":
                    renderer.material.color = materialColors["iron-ore"];
                    InstantiateOre("iron-ore", new Vector3((float)X, height + 2, (float)Z));
                    elementCounter.IncrementCount(Elements.IronOre);
                    break;
                case "copper-ore":
                    renderer.material.color = materialColors["copper-ore"];
                    InstantiateOre("copper-ore", new Vector3((float)X, height + 2, (float)Z));
                    elementCounter.IncrementCount(Elements.CopperOre);
                    break;
                case "gold-ore":
                    renderer.material.color = materialColors["gold-ore"];
                    InstantiateOre("gold-ore", new Vector3((float)X, height + 2, (float)Z));
                    elementCounter.IncrementCount(Elements.GoldOre);
                    break;
                case "uranium-ore":
                    renderer.material.color = materialColors["uranium-ore"];
                    InstantiateOre("uranium-ore", new Vector3((float)X, height + 2, (float)Z));
                    elementCounter.IncrementCount(Elements.UraniumOre);
                    break;
                case "stone":
                    renderer.material.color = materialColors["stone"];
                    InstantiateOre("stone", new Vector3((float)X, height + 2, (float)Z));
                    elementCounter.IncrementCount(Elements.Stone);
                    break;
                case "oil":
                    renderer.material.color = materialColors["oil"];
                    elementCounter.IncrementCount(Elements.Oil);
                    break;
                case "natural-gas":
                    renderer.material.color = materialColors["natural-gas"];
                    elementCounter.IncrementCount(Elements.NaturalGas);
                    break;
                case "tree":
                    renderer.material.color = materialColors["tree"];
                    elementCounter.IncrementCount(Elements.Tree);
                    break;
                default:
                    break;
            }
        }

        tiles[x, y] = baseTile;
        return baseTile;
    }


    // Regenerates the grid with new tiles and resources.
    public void RegenerateGrid()
    {
        Debug.Log("Regenerating grid...");

        // Initialize tile data
        tileData = new TileData[gridLength, gridWidth];

        // Clear dictionary
        tileDataDictionary.Clear();

        // Destroy existing tiles if any
        ClearGrid();

        // Generate noise map for random terrain generation
        var noiseMap = GenerateNoiseMap(gridWidth, gridLength, scaleFactor, Random.Range(0, 1000000), octaveAmount, persAmount, lacuAmount);

        // Initialize game object arrays
        tiles = new GameObject[gridLength, gridWidth];
        resources = new GameObject[gridLength, gridWidth];

        // Set up new tiles and resources
        SetUpTilesAndResources(noiseMap);

        Debug.Log("Grid regeneration complete.");
    }

    private void ClearGrid()
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        if (tiles == null) return;

        for (int i = 0; i < gridLength * gridWidth; i++)
        {
            int q = i / gridWidth;
            int r = i % gridWidth;

            if (tiles[q, r] != null)
            {
                Destroy(tiles[q, r]);
                tiles[q, r] = null;
            }
        }
        stopWatch.Stop();
        Debug.Log($"Cleargrid execution time: {stopWatch.ElapsedMilliseconds}");
    }

    private void SetUpTilesAndResources(float[,] noiseMap)
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        for (int i = 0; i < gridLength * gridWidth; i++)
        {
            int q = i / gridWidth;
            int r = i % gridWidth;

            var tileID = i;
            var perlinValue = noiseMap[q, r];
            var resourceType = DetermineResourceType(perlinValue);
            tileData[q, r] = new TileData(tileID, q, r, resourceType);
            tileDataDictionary[tileID] = tileData[q, r];
            SetupTile(q, r, tileID, resourceType, 0);
        }

        stopWatch.Stop();
        Debug.Log($"Cleargrid execution time: {stopWatch.ElapsedMilliseconds}");
    }

    public string DetermineResourceType(float perlinValue)
    {
        return perlinValue < 0.1f ? "water"
            : perlinValue < 0.15f ? "uranium-ore"
            : perlinValue < 0.2f ? "gold-ore"
            : perlinValue < 0.3f ? "coal-ore"
            : perlinValue < 0.4f ? "iron-ore"
            : perlinValue < 0.5f ? "copper-ore"
            : perlinValue < 0.6f ? "oil"
            : perlinValue < 0.75f ? "earth"
            : perlinValue < 0.85f ? "stone"
            : perlinValue < 0.95f ? "natural-gas"
            : "tree";
    }


    public void DeleteGrid()
    {
        if (tiles != null)
        {
            for (int i = 0; i < gridLength * gridWidth; i++)
            {
                int q = i / gridWidth;
                int r = i % gridWidth;

                if (tiles[q, r] != null)
                {
                    Debug.Log($"Destroying tile at [{q},{r}] with name {tiles[q, r].name}");
                    Destroy(tiles[q, r]);
                    tiles[q, r] = null; // Set the reference to null after destroying the object
                }
            }
        }
    }

    public void SaveWorldData(string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            foreach (var tile in tileDataDictionary.Values)
            {
                writer.WriteLine($"{tile.TileID},{tile.x},{tile.z},{tile.resourceType}");
            }
        }
    }

    public void LoadWorldData(string filePath)
    {
        Debug.Log("Loading world data");

        // Initialize or clear existing data
        tileData = new TileData[gridLength, gridWidth];
        tileDataDictionary.Clear();

        // Destroy existing tiles if any
        if (tiles != null)
        {
            for (int q = 0; q < gridLength; q++)
            {
                for (int r = 0; r < gridWidth; r++)
                {
                    if (tiles[q, r] != null)
                    {
                        Destroy(tiles[q, r]);
                        tiles[q, r] = null; // Set the reference to null after destroying the object
                    }
                }
            }
        }

        tiles = new GameObject[gridLength, gridWidth];
        resources = new GameObject[gridLength, gridWidth];

        using (var reader = new StreamReader(filePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');
                var tile = new TileData
                {
                    TileID = int.Parse(parts[0]),
                    x = int.Parse(parts[1]),
                    z = int.Parse(parts[2]),
                    resourceType = parts[3] // Now simply storing the resource type as string
                };

                tileData[tile.x, tile.z] = tile;
                tileDataDictionary[tile.TileID] = tile;

                // Instantiate the tile GameObject and set its properties
                SetupTile(tile.x, tile.z, tile.TileID, tile.resourceType, 0);
            }
        }
        Debug.Log("World data loaded and grid generated.");
    }
}