using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public static class LevelCampaignBuilder
{
    private const float MapWidthMultiplier = 5.5f;
    private const float OverviewOrthographicSize = 38f;
    private const float TerrainFloorY = -45f;
    private const string LevelOnePath = "Assets/Prefabs/Level_1.prefab";
    private const string SafePadPath = "Assets/Prefabs/LandingPad.prefab";
    private const string BonusPadPath = "Assets/Prefabs/LandingPad_x5.prefab";
    private const string CoinPath = "Assets/Prefabs/CoinPickup.prefab";
    private const string FuelPath = "Assets/Prefabs/FuelPickup.prefab";
    private readonly struct Pad
    {
        public readonly bool Bonus;
        public readonly Vector2 Position;

        public Pad(bool bonus, float x, float y)
        {
            Bonus = bonus;
            Position = new Vector2(x, y);
        }
    }

    private sealed class Level
    {
        public int Number;
        public string TerrainName;
        public Vector2 Start;
        public Vector2[] Surface;
        public Pad[] Pads;
        public Vector2[] Coins;
        public Vector2[] Fuel;
    }

    [MenuItem("Tools/Lua Lander/Build Level Campaign")]
    public static void Build()
    {
        GameObject levelOne = Load<GameObject>(LevelOnePath);
        SpriteShapeController terrainSource = null;
        foreach (SpriteShapeController candidate in levelOne.GetComponentsInChildren<SpriteShapeController>())
        {
            if (candidate.GetComponent<Collider2D>() != null)
            {
                terrainSource = candidate;
                break;
            }
        }

        if (terrainSource == null)
        {
            throw new InvalidOperationException("Level_1 needs a SpriteShape terrain with a Collider2D.");
        }

        GameObject terrainTemplate = UnityEngine.Object.Instantiate(terrainSource.gameObject);
        terrainTemplate.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            GameObject safePad = Load<GameObject>(SafePadPath);
            GameObject bonusPad = Load<GameObject>(BonusPadPath);
            GameObject coin = Load<GameObject>(CoinPath);
            GameObject fuel = Load<GameObject>(FuelPath);

            foreach (Level level in GetLevels())
            {
                BuildLevel(level, terrainTemplate, safePad, bonusPad, coin, fuel);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Built Level_1 through Level_10 as vast, sparse space journeys.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(terrainTemplate);
        }
    }

    private static void BuildLevel(
        Level level,
        GameObject terrainTemplate,
        GameObject safePad,
        GameObject bonusPad,
        GameObject coin,
        GameObject fuel)
    {
        GameObject root = new GameObject($"Level_{level.Number}");

        try
        {
            GameLevel gameLevel = root.AddComponent<GameLevel>();

            AddDistantHorizon(level.Number, terrainTemplate, root.transform);

            GameObject terrain = UnityEngine.Object.Instantiate(terrainTemplate, root.transform);
            terrain.hideFlags = HideFlags.None;
            terrain.name = level.TerrainName;
            terrain.transform.localPosition = Vector3.zero;
            terrain.transform.localRotation = Quaternion.identity;
            Vector2[] surface = Array.ConvertAll(level.Surface, SpreadPosition);
            ConfigureTerrain(terrain, Ruggedize(surface, level.Pads, level.Number, 0.7f, 3f), true);

            GameObject start = new GameObject("LanderStartPosition");
            start.transform.SetParent(root.transform, false);
            start.transform.localPosition = SpreadPosition(level.Start);

            GameObject cameraTarget = new GameObject("CameraStartTarget");
            cameraTarget.transform.SetParent(root.transform, false);

            for (int i = 0; i < level.Pads.Length; i++)
            {
                Pad pad = level.Pads[i];
                GameObject prefab = pad.Bonus ? bonusPad : safePad;
                string name = pad.Bonus ? $"BonusPad_x5_{i + 1:00}" : $"SafePad_x1_{i + 1:00}";
                AddPrefab(prefab, root.transform, name, SpreadPosition(pad.Position));
            }

            for (int i = 0; i < level.Coins.Length; i += 2)
            {
                AddPrefab(coin, root.transform, $"Coin_{i / 2 + 1:00}", SpreadPosition(level.Coins[i]));
            }

            for (int i = 0; i < level.Fuel.Length; i++)
            {
                AddPrefab(fuel, root.transform, $"Fuel_{i + 1:00}", SpreadPosition(level.Fuel[i]));
            }

            SerializedObject serializedLevel = new SerializedObject(gameLevel);
            serializedLevel.FindProperty("levelNumber").intValue = level.Number;
            serializedLevel.FindProperty("landerStartPositionTransform").objectReferenceValue = start.transform;
            serializedLevel.FindProperty("cameraStartTagetTransform").objectReferenceValue = cameraTarget.transform;
            serializedLevel.FindProperty("zoomedOutOrthographicSize").floatValue = OverviewOrthographicSize;
            serializedLevel.ApplyModifiedPropertiesWithoutUndo();

            string path = $"Assets/Prefabs/Level_{level.Number}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success)
            {
                throw new InvalidOperationException($"Could not save {path}.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ConfigureTerrain(GameObject terrain, Vector2[] surface, bool collidable)
    {
        SpriteShapeController controller = terrain.GetComponent<SpriteShapeController>();
        if (controller == null)
        {
            throw new InvalidOperationException("Terrain template has no SpriteShapeController.");
        }

        Spline spline = controller.spline;
        spline.Clear();

        Vector2[] polygon = new Vector2[surface.Length + 2];
        polygon[0] = new Vector2(surface[0].x, TerrainFloorY);
        Array.Copy(surface, 0, polygon, 1, surface.Length);
        polygon[^1] = new Vector2(surface[^1].x, TerrainFloorY);

        for (int i = 0; i < polygon.Length; i++)
        {
            spline.InsertPointAt(i, polygon[i]);
            spline.SetTangentMode(i, ShapeTangentMode.Linear);
            spline.SetCorner(i, true);
            spline.SetHeight(i, 1f);
        }

        spline.isOpenEnded = false;
        controller.RefreshSpriteShape();
        controller.autoUpdateCollider = collidable;

        if (collidable)
        {
            controller.BakeCollider();
        }
        else
        {
            Collider2D collider = terrain.GetComponent<Collider2D>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }
    }

    private static void AddDistantHorizon(int levelNumber, GameObject terrainTemplate, Transform parent)
    {
        GameObject horizon = UnityEngine.Object.Instantiate(terrainTemplate, parent);
        horizon.hideFlags = HideFlags.None;
        horizon.name = "DistantHorizon";
        horizon.transform.localPosition = Vector3.zero;
        horizon.transform.localRotation = Quaternion.identity;
        ConfigureTerrain(
            horizon,
            Ruggedize(GetHorizon(levelNumber), Array.Empty<Pad>(), levelNumber + 100, 1.4f, 3.5f),
            false);

        SpriteShapeRenderer renderer = horizon.GetComponent<SpriteShapeRenderer>();
        renderer.color = new Color(0.12f, 0.18f, 0.28f, 0.72f);
        renderer.sortingOrder = -10;
    }

    private static Vector2[] GetHorizon(int levelNumber)
    {
        float direction = levelNumber % 2 == 0 ? 1f : -1f;
        return Array.ConvertAll(new[]
        {
            new Vector2(-72f, -4f), new Vector2(-64f, 1f),
            new Vector2(-55f, 13f), new Vector2(-47f, 3f),
            new Vector2(-37f, 7f), new Vector2(-29f, 18f),
            new Vector2(-18f, 5f), new Vector2(-7f, 10f),
            new Vector2(4f, 4f), new Vector2(15f, 16f),
            new Vector2(25f, 6f), new Vector2(36f, 2f),
            new Vector2(46f, 19f), new Vector2(54f, 7f),
            new Vector2(64f, 1f), new Vector2(72f, -4f),
        }, point => new Vector2(point.x * direction, point.y));
    }

    private static Vector2[] Ruggedize(
        Vector2[] surface,
        Pad[] pads,
        int seed,
        float amplitude,
        float maxSegmentLength)
    {
        System.Random random = new System.Random(seed * 7919);
        Vector2[] anchors = (Vector2[])surface.Clone();

        for (int i = 1; i < anchors.Length - 1; i++)
        {
            if (!IsLandingZone(anchors[i].x, pads))
            {
                anchors[i].y += RandomRange(random, -amplitude, amplitude);
            }
        }

        List<Vector2> points = new List<Vector2>(anchors.Length * 3) { anchors[0] };
        for (int i = 0; i < anchors.Length - 1; i++)
        {
            Vector2 from = anchors[i];
            Vector2 to = anchors[i + 1];
            int segments = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(to.x - from.x) / maxSegmentLength));

            for (int step = 1; step < segments; step++)
            {
                float t = (step + RandomRange(random, -0.22f, 0.22f)) / segments;
                Vector2 point = Vector2.Lerp(from, to, t);

                if (!IsLandingZone(point.x, pads))
                {
                    point.y += RandomRange(random, -amplitude, amplitude);
                    if (random.NextDouble() < 0.16)
                    {
                        point.y -= amplitude * 1.4f;
                    }
                }

                points.Add(point);
            }

            points.Add(to);
        }

        return points.ToArray();
    }

    private static bool IsLandingZone(float x, Pad[] pads)
    {
        foreach (Pad pad in pads)
        {
            if (Mathf.Abs(x - SpreadPosition(pad.Position).x) < 4f)
            {
                return true;
            }
        }

        return false;
    }

    private static float RandomRange(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private static Vector2 SpreadPosition(Vector2 position)
    {
        return new Vector2(position.x * MapWidthMultiplier, position.y);
    }

    private static void AddPrefab(GameObject prefab, Transform parent, string name, Vector2 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.identity;
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new InvalidOperationException($"Missing asset: {path}");
        }

        return asset;
    }

    private static Level[] GetLevels()
    {
        return new[]
        {
            new Level
            {
                Number = 1,
                TerrainName = "Terrain_FirstContact",
                Start = new Vector2(-8.4f, 4.2f),
                Surface = new[]
                {
                    new Vector2(-10.5f, -2.8f), new Vector2(-9f, -2.8f),
                    new Vector2(-7.8f, 2.8f), new Vector2(-6.5f, 5.8f),
                    new Vector2(-5.1f, 0.6f), new Vector2(-3.4f, -3.5f),
                    new Vector2(-1.2f, -4.2f), new Vector2(1.5f, -4.2f),
                    new Vector2(3.4f, -2f), new Vector2(4.9f, 4.8f),
                    new Vector2(6.1f, 8.5f), new Vector2(7.2f, 1.2f),
                    new Vector2(8.5f, -3f), new Vector2(10.5f, -3f),
                },
                Pads = new[]
                {
                    new Pad(true, 6.1f, 8.55f),
                    new Pad(false, 9.4f, -2.95f),
                },
                Coins = new[]
                {
                    new Vector2(-5.5f, 4f), new Vector2(-1f, 0.5f),
                    new Vector2(3.6f, 1.2f), new Vector2(6.1f, 10f),
                },
                Fuel = new[] { new Vector2(0f, -0.5f) },
            },
            new Level
            {
                Number = 2,
                TerrainName = "Terrain_WideBasin",
                Start = new Vector2(-6.3f, 3.6f),
                Surface = new[]
                {
                    new Vector2(-10.5f, -1.2f), new Vector2(-7.5f, -1.2f),
                    new Vector2(-5f, -1.8f), new Vector2(-2.5f, -2.5f),
                    new Vector2(0f, -2.7f), new Vector2(2f, -2.7f),
                    new Vector2(7f, -2.7f), new Vector2(10.5f, -1.4f),
                },
                Pads = new[] { new Pad(false, 4.5f, -2.65f) },
                Coins = new[]
                {
                    new Vector2(-4.7f, 2.4f), new Vector2(-1.8f, 1.1f),
                    new Vector2(1.1f, -0.1f),
                },
                Fuel = Array.Empty<Vector2>(),
            },
            new Level
            {
                Number = 3,
                TerrainName = "Terrain_RiskOrReward",
                Start = new Vector2(0f, 3.7f),
                Surface = new[]
                {
                    new Vector2(-10.5f, -1.3f), new Vector2(-5.2f, -1.3f),
                    new Vector2(-3.5f, -2.4f), new Vector2(0f, -2.8f),
                    new Vector2(2.5f, -1.7f), new Vector2(4.5f, 0.2f),
                    new Vector2(6f, 1.5f), new Vector2(8.4f, 1.5f),
                    new Vector2(10.5f, -0.8f),
                },
                Pads = new[]
                {
                    new Pad(false, -7.85f, -1.25f),
                    new Pad(true, 7.2f, 1.55f),
                },
                Coins = new[]
                {
                    new Vector2(2f, 2.7f), new Vector2(4.4f, 2.25f),
                    new Vector2(7.2f, 2.9f),
                },
                Fuel = new[] { new Vector2(0f, 0.2f) },
            },
            new Level
            {
                Number = 4,
                TerrainName = "Terrain_MountainTransfer",
                Start = new Vector2(-7.6f, 3.55f),
                Surface = new[]
                {
                    new Vector2(-10.5f, -2.4f), new Vector2(-7.2f, -2.4f),
                    new Vector2(-5.4f, -1.2f), new Vector2(-2.4f, 2.7f),
                    new Vector2(-0.2f, 3.55f), new Vector2(2.2f, 2.65f),
                    new Vector2(4.3f, -1.2f), new Vector2(5.3f, -2.7f),
                    new Vector2(10.5f, -2.7f),
                },
                Pads = new[] { new Pad(false, 7.9f, -2.65f) },
                Coins = new[]
                {
                    new Vector2(-4.9f, 2.2f), new Vector2(-2.8f, 3.8f),
                    new Vector2(0f, 4.45f), new Vector2(2.8f, 3.65f),
                    new Vector2(4.8f, 1.3f),
                },
                Fuel = new[] { new Vector2(3.9f, 2.55f) },
            },
            new Level
            {
                Number = 5,
                TerrainName = "Terrain_CraterDescent",
                Start = new Vector2(-6.5f, 3.7f),
                Surface = new[]
                {
                    new Vector2(-10.5f, 1.3f), new Vector2(-7f, 1.3f),
                    new Vector2(-5f, -0.4f), new Vector2(-3f, -2.5f),
                    new Vector2(-1.2f, -3.2f), new Vector2(1.2f, -3.2f),
                    new Vector2(3f, -2.5f), new Vector2(5f, -0.4f),
                    new Vector2(7f, 1.3f), new Vector2(10.5f, 1.3f),
                },
                Pads = new[] { new Pad(true, 0f, -3.15f) },
                Coins = new[]
                {
                    new Vector2(-4.8f, 2.45f), new Vector2(-3f, 1.15f),
                    new Vector2(-1.6f, -0.25f), new Vector2(0f, -1.55f),
                },
                Fuel = new[] { new Vector2(2.8f, -0.25f) },
            },
            new Level
            {
                Number = 6,
                TerrainName = "Terrain_FuelRun",
                Start = new Vector2(-8f, 3.6f),
                Surface = new[]
                {
                    new Vector2(-10.5f, -2.5f), new Vector2(-7.2f, -2.5f),
                    new Vector2(-5.2f, -0.2f), new Vector2(-3.3f, 2.35f),
                    new Vector2(-1.1f, -0.2f), new Vector2(0.5f, -2.2f),
                    new Vector2(2.8f, 1.9f), new Vector2(4.5f, -0.2f),
                    new Vector2(5.2f, -2.6f), new Vector2(10.5f, -2.6f),
                },
                Pads = new[] { new Pad(false, 7.85f, -2.55f) },
                Coins = new[]
                {
                    new Vector2(-6.1f, 1.8f), new Vector2(-3.5f, 3.35f),
                    new Vector2(-0.8f, 1.2f), new Vector2(2.8f, 3f),
                    new Vector2(5f, 1.1f),
                },
                Fuel = new[]
                {
                    new Vector2(-0.3f, 1.55f), new Vector2(4.35f, 2f),
                },
            },
            new Level
            {
                Number = 7,
                TerrainName = "Terrain_SawtoothPass",
                Start = new Vector2(-8.2f, 3.7f),
                Surface = new[]
                {
                    new Vector2(-10.5f, -2.9f), new Vector2(-8.2f, -2.9f),
                    new Vector2(-6.2f, 1.8f), new Vector2(-4.3f, -2.4f),
                    new Vector2(-2.1f, 2.65f), new Vector2(0f, -2.5f),
                    new Vector2(2.1f, 2.05f), new Vector2(4.2f, -2.5f),
                    new Vector2(5f, -2.9f), new Vector2(10.5f, -2.9f),
                },
                Pads = new[] { new Pad(false, 7.75f, -2.85f) },
                Coins = new[]
                {
                    new Vector2(-6.3f, 2.85f), new Vector2(-4.2f, 1.6f),
                    new Vector2(-2f, 3.65f), new Vector2(0f, 1.45f),
                    new Vector2(2.2f, 3.05f), new Vector2(4.2f, 1.1f),
                },
                Fuel = new[] { new Vector2(0f, 3.5f) },
            },
            new Level
            {
                Number = 8,
                TerrainName = "Terrain_TowerChoice",
                Start = new Vector2(7.3f, 3.7f),
                Surface = new[]
                {
                    new Vector2(-10.5f, -2.8f), new Vector2(-5.2f, -2.8f),
                    new Vector2(-3.8f, -1.2f), new Vector2(-2f, 0.8f),
                    new Vector2(-1.2f, 2f), new Vector2(1.2f, 2f),
                    new Vector2(2.2f, 0.7f), new Vector2(4.5f, -1.5f),
                    new Vector2(7f, -2.2f), new Vector2(10.5f, -2.2f),
                },
                Pads = new[]
                {
                    new Pad(false, -7.85f, -2.75f),
                    new Pad(true, 0f, 2.05f),
                },
                Coins = new[]
                {
                    new Vector2(5.2f, 2.45f), new Vector2(3.1f, 2.9f),
                    new Vector2(0f, 3.25f), new Vector2(-2.7f, 2.1f),
                    new Vector2(-4.5f, 0.35f),
                },
                Fuel = Array.Empty<Vector2>(),
            },
            new Level
            {
                Number = 9,
                TerrainName = "Terrain_DeepShaft",
                Start = new Vector2(-7.5f, 3.8f),
                Surface = new[]
                {
                    new Vector2(-10.5f, 2.55f), new Vector2(-7.2f, 2.55f),
                    new Vector2(-5f, 0.8f), new Vector2(-3f, -1.8f),
                    new Vector2(-1.15f, -3.3f), new Vector2(1.15f, -3.3f),
                    new Vector2(3f, -1.8f), new Vector2(5f, 0.8f),
                    new Vector2(7.2f, 2.55f), new Vector2(10.5f, 2.55f),
                },
                Pads = new[] { new Pad(true, 0f, -3.25f) },
                Coins = new[]
                {
                    new Vector2(-5.9f, 3.55f), new Vector2(-4.1f, 2.15f),
                    new Vector2(-2.5f, 0.55f), new Vector2(-1.1f, -1.25f),
                    new Vector2(0f, -2.05f),
                },
                Fuel = new[] { new Vector2(2.8f, 0.15f) },
            },
            new Level
            {
                Number = 10,
                TerrainName = "Terrain_FinalApproach",
                Start = new Vector2(-8f, 3.8f),
                Surface = new[]
                {
                    new Vector2(-10.5f, -2.6f), new Vector2(-8f, -2.6f),
                    new Vector2(-6.2f, -0.2f), new Vector2(-4.3f, 2.7f),
                    new Vector2(-2.2f, 0.7f), new Vector2(-0.2f, -2.4f),
                    new Vector2(0.5f, -2.4f), new Vector2(2.7f, -2.4f),
                    new Vector2(4f, -0.2f), new Vector2(5.4f, 2.85f),
                    new Vector2(7.1f, -0.8f), new Vector2(7.5f, -2.9f),
                    new Vector2(10.5f, -2.9f),
                },
                Pads = new[]
                {
                    new Pad(true, 1.6f, -2.35f),
                    new Pad(false, 9f, -2.85f),
                },
                Coins = new[]
                {
                    new Vector2(-6.4f, 1.6f), new Vector2(-4.5f, 3.75f),
                    new Vector2(-2.5f, 2.35f), new Vector2(-0.8f, 0.25f),
                    new Vector2(1.6f, -1.15f), new Vector2(4.2f, 1.7f),
                },
                Fuel = new[] { new Vector2(-1.3f, 3.45f) },
            },
        };
    }
}
