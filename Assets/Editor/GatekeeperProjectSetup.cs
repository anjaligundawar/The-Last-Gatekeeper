using System.Collections.Generic;
using System.IO;
using Gatekeeper;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// One-time setup: Gatekeeper > Create Project Assets.
/// Creates placeholder sprites, data assets, prefabs and the Gatekeeper scene, all wired up.
/// After it has run, everything is ordinary project assets you edit in the Inspector.
/// You can delete this script afterwards.
/// </summary>
public static class GatekeeperProjectSetup
{
    const string ArtFolder = "Assets/Art/Shapes";
    const string DataFolder = "Assets/Data";
    const string PrefabFolder = "Assets/Prefabs";
    const string ScenePath = "Assets/Scenes/Gatekeeper.unity";

    static Sprite square, circle, diamond, triangle, ring;

    static readonly Color NormalButton = new Color(0.22f, 0.24f, 0.28f, 0.95f);
    static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.72f);

    [MenuItem("Gatekeeper/Create Project Assets")]
    static void Run()
    {
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            EditorUtility.DisplayDialog("Import TextMesh Pro first",
                "Run Window > TextMeshPro > Import TMP Essential Resources, then run Gatekeeper > Create Project Assets again.", "OK");
            return;
        }

        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Overwrite?", "The Gatekeeper scene, prefabs and data assets already exist. Recreate them?", "Recreate", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (var folder in new[] { ArtFolder, DataFolder + "/Enemies", DataFolder + "/Towers", DataFolder + "/Weapons",
                     PrefabFolder + "/Enemies", PrefabFolder + "/Towers", PrefabFolder + "/Projectiles", PrefabFolder + "/Effects", "Assets/Scenes" })
            EnsureFolder(folder);

        CreateSprites();
        var data = CreateData();
        var prefabs = CreatePrefabs(data);
        CreateScene(data, prefabs);

        AssetDatabase.SaveAssets();
        Debug.Log("[Gatekeeper] Project assets created. Open " + ScenePath + " and press Play.");
    }

    // ---------------------------------------------------------------- sprites

    static void CreateSprites()
    {
        square = MakeSprite("Square", (x, y) => true);
        circle = MakeSprite("Circle", (x, y) => x * x + y * y <= 1f);
        diamond = MakeSprite("Diamond", (x, y) => Mathf.Abs(x) + Mathf.Abs(y) <= 1f);
        triangle = MakeSprite("Triangle", (x, y) => y >= -0.9f && Mathf.Abs(x) <= (1f - y) * 0.5f);
        ring = MakeSprite("Ring", (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return d <= 1f && d >= 0.92f; });
    }

    static Sprite MakeSprite(string name, System.Func<float, float, bool> inside)
    {
        const int size = 64;
        string path = ArtFolder + "/" + name + ".png";
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * 2f - 1f;
                float y = (py + 0.5f) / size * 2f - 1f;
                pixels[py * size + px] = inside(x, y) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ------------------------------------------------------------------- data

    class DataSet
    {
        public GameConfig config;
        public EnemyData[] enemies;
        public TowerData[] towers;
        public WeaponData[] weapons;
    }

    static DataSet CreateData()
    {
        var set = new DataSet();
        set.config = Asset<GameConfig>(DataFolder + "/GameConfig.asset", c => { });

        set.enemies = new[]
        {
            Asset<EnemyData>(DataFolder + "/Enemies/Grunt.asset", e => FillEnemy(e, EnemyType.Grunt, "Grunt", 30, 1.4f, 5, 1f, 1, false, 1f, 1f)),
            Asset<EnemyData>(DataFolder + "/Enemies/Runner.asset", e => FillEnemy(e, EnemyType.Runner, "Runner", 15, 2.8f, 3, 0.7f, 1, false, 1f, 1f)),
            Asset<EnemyData>(DataFolder + "/Enemies/Shield.asset", e => FillEnemy(e, EnemyType.Shield, "Shield", 60, 1f, 6, 1.2f, 2, false, 0.25f, 1.25f)),
            Asset<EnemyData>(DataFolder + "/Enemies/Flyer.asset", e => FillEnemy(e, EnemyType.Flyer, "Flyer", 20, 1.8f, 4, 1f, 2, true, 1f, 1f)),
        };

        set.towers = new[]
        {
            Asset<TowerData>(DataFolder + "/Towers/Archer.asset", t =>
            {
                t.type = TowerType.Archer; t.displayName = "Archer"; t.description = "Single target, hits air";
                t.uiColor = new Color(0.45f, 0.85f, 0.45f); t.range = 3.5f; t.interval = 0.8f; t.damage = 8;
                t.damageType = DamageType.Arrow; t.hitsAir = true; t.splashRadius = 0; t.slowFactor = 1; t.slowDuration = 0;
            }),
            Asset<TowerData>(DataFolder + "/Towers/Cannon.asset", t =>
            {
                t.type = TowerType.Cannon; t.displayName = "Cannon"; t.description = "Splash damage, ground only";
                t.uiColor = new Color(0.7f, 0.7f, 0.7f); t.range = 3.2f; t.interval = 1.8f; t.damage = 14;
                t.damageType = DamageType.Explosive; t.hitsAir = false; t.splashRadius = 1f; t.slowFactor = 1; t.slowDuration = 0;
            }),
            Asset<TowerData>(DataFolder + "/Towers/Frost.asset", t =>
            {
                t.type = TowerType.Frost; t.displayName = "Frost"; t.description = "Slows ground enemies";
                t.uiColor = new Color(0.55f, 0.9f, 1f); t.range = 2.5f; t.interval = 1f; t.damage = 3;
                t.damageType = DamageType.Frost; t.hitsAir = false; t.splashRadius = 0; t.slowFactor = 0.5f; t.slowDuration = 1.5f;
            }),
        };

        set.weapons = new[]
        {
            Asset<WeaponData>(DataFolder + "/Weapons/Bow.asset", w =>
            {
                w.type = WeaponType.Bow; w.displayName = "Bow"; w.description = "Long range, low damage, hits flyers";
                w.range = 6f; w.cooldown = 0.45f; w.damage = 8; w.damageType = DamageType.Arrow; w.hitsAir = true; w.width = 0;
            }),
            Asset<WeaponData>(DataFolder + "/Weapons/Sword.asset", w =>
            {
                w.type = WeaponType.Sword; w.displayName = "Sword"; w.description = "Short range, high damage, can't hit flyers";
                w.range = 1.3f; w.cooldown = 0.4f; w.damage = 18; w.damageType = DamageType.Physical; w.hitsAir = false; w.width = 0;
            }),
            Asset<WeaponData>(DataFolder + "/Weapons/Spear.asset", w =>
            {
                w.type = WeaponType.Spear; w.displayName = "Spear"; w.description = "Medium range, pierces a line of enemies";
                w.range = 2.8f; w.cooldown = 0.6f; w.damage = 12; w.damageType = DamageType.Physical; w.hitsAir = true; w.width = 0.5f;
            }),
        };
        return set;
    }

    static void FillEnemy(EnemyData e, EnemyType type, string name, float hp, float speed, float damage, float interval, int cost,
        bool flying, float arrowMultiplier, float explosiveMultiplier)
    {
        e.type = type; e.displayName = name; e.maxHp = hp; e.speed = speed; e.damage = damage; e.attackInterval = interval;
        e.cost = cost; e.flying = flying; e.arrowMultiplier = arrowMultiplier; e.explosiveMultiplier = explosiveMultiplier;
        e.detectRange = 3f; e.loseRange = 5f; e.attackRange = 0.8f;
    }

    static T Asset<T>(string path, System.Action<T> fill) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            fill(asset);
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            fill(asset);
            EditorUtility.SetDirty(asset);
        }
        return asset;
    }

    // ---------------------------------------------------------------- prefabs

    class PrefabSet
    {
        public Hero hero;
        public Enemy[] enemies;
        public Tower[] towers;
    }

    static readonly Color[] EnemyColors =
    {
        new Color(0.85f, 0.35f, 0.3f), new Color(1f, 0.65f, 0.2f), new Color(0.45f, 0.6f, 0.85f), new Color(0.75f, 0.45f, 0.95f),
    };
    static readonly float[] EnemySizes = { 0.45f, 0.35f, 0.55f, 0.4f };

    static PrefabSet CreatePrefabs(DataSet data)
    {
        var deathFx = Effect("Fx_Death", circle, new Color(1f, 1f, 1f, 0.6f), 0.15f);
        var explosionFx = Effect("Fx_Explosion", circle, new Color(1f, 0.6f, 0.2f, 0.5f), 0.2f);
        var slashFx = Effect("Fx_SwordSlash", circle, new Color(1f, 1f, 1f, 0.35f), 0.1f);
        var thrustFx = Effect("Fx_SpearThrust", square, new Color(1f, 1f, 1f, 0.45f), 0.1f);
        var frostFx = Effect("Fx_FrostPulse", ring, new Color(0.6f, 0.9f, 1f, 0.6f), 0.3f);

        var arrow = ProjectilePrefab("Arrow", diamond, new Color(1f, 0.95f, 0.6f), 0.18f, 12f, null);
        var cannonball = ProjectilePrefab("Cannonball", circle, new Color(0.2f, 0.2f, 0.2f), 0.3f, 7f, explosionFx);

        var set = new PrefabSet { enemies = new Enemy[4], towers = new Tower[3] };
        Sprite[] enemyShapes = { square, triangle, square, circle };
        for (int i = 0; i < 4; i++)
            set.enemies[i] = EnemyPrefab(data.enemies[i], enemyShapes[i], EnemyColors[i], EnemySizes[i], deathFx);

        Sprite[] towerShapes = { diamond, circle, square };
        Projectile[] towerShots = { arrow, cannonball, null };
        for (int i = 0; i < 3; i++)
            set.towers[i] = TowerPrefab(data.towers[i], towerShapes[i], towerShots[i], i == (int)TowerType.Frost ? frostFx : null);

        set.hero = HeroPrefab(data, arrow, slashFx, thrustFx);
        return set;
    }

    static FadeOut Effect(string name, Sprite sprite, Color color, float duration)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 35;
        var fx = go.AddComponent<FadeOut>();
        SetFloat(fx, "duration", duration);
        return SavePrefab(go, PrefabFolder + "/Effects/" + name + ".prefab").GetComponent<FadeOut>();
    }

    static Projectile ProjectilePrefab(string name, Sprite sprite, Color color, float scale, float speed, FadeOut impact)
    {
        var go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 30;
        var projectile = go.AddComponent<Projectile>();
        SetFloat(projectile, "speed", speed);
        Set(projectile, "impactEffect", impact);
        return SavePrefab(go, PrefabFolder + "/Projectiles/" + name + ".prefab").GetComponent<Projectile>();
    }

    static Enemy EnemyPrefab(EnemyData data, Sprite shape, Color color, float size, FadeOut deathFx)
    {
        var go = new GameObject(data.displayName);
        var health = go.AddComponent<Health>();
        SetFloat(health, "maxHp", data.maxHp);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = size * 0.5f;

        if (data.flying)
            SpriteObject("Shadow", go.transform, circle, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -0.35f), new Vector2(size * 0.8f, size * 0.3f), 9);
        var body = SpriteObject("Body", go.transform, shape, color, Vector2.zero, Vector2.one * size, data.flying ? 15 : 10);
        var icon = SpriteObject("StateIcon", go.transform, triangle, new Color(1f, 0.75f, 0.1f), new Vector2(0f, size * 0.5f + 0.45f), Vector2.one * 0.25f, 40);
        icon.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        var barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(go.transform, false);
        barRoot.transform.localPosition = new Vector2(0f, size * 0.5f + 0.15f);
        var back = SpriteObject("Background", barRoot.transform, square, new Color(0f, 0f, 0f, 0.6f), Vector2.zero, new Vector2(0.6f, 0.08f), 41);
        var fill = SpriteObject("Fill", back.transform, square, new Color(0.3f, 1f, 0.3f), Vector2.zero, Vector2.one, 42);
        var bar = barRoot.AddComponent<HealthBar>();
        Set(bar, "health", health);
        Set(bar, "fill", fill.transform);

        var enemy = go.AddComponent<Enemy>();
        Set(enemy, "data", data);
        Set(enemy, "body", body);
        Set(enemy, "stateIcon", icon);
        Set(enemy, "deathEffect", deathFx);
        return SavePrefab(go, PrefabFolder + "/Enemies/" + data.displayName + ".prefab").GetComponent<Enemy>();
    }

    static Tower TowerPrefab(TowerData data, Sprite shape, Projectile projectile, FadeOut pulse)
    {
        var go = new GameObject(data.displayName + " Tower");
        SpriteObject("Body", go.transform, shape, data.uiColor, Vector2.zero, Vector2.one * 0.75f, 0);
        var range = SpriteObject("RangeIndicator", go.transform, ring, new Color(data.uiColor.r, data.uiColor.g, data.uiColor.b, 0.35f),
            Vector2.zero, Vector2.one * data.range * 2f, -35);
        var tower = go.AddComponent<Tower>();
        Set(tower, "data", data);
        Set(tower, "rangeIndicator", range);
        Set(tower, "projectilePrefab", projectile);
        Set(tower, "pulseEffect", pulse);
        return SavePrefab(go, PrefabFolder + "/Towers/" + data.displayName + ".prefab").GetComponent<Tower>();
    }

    static Hero HeroPrefab(DataSet data, Projectile arrow, FadeOut slash, FadeOut thrust)
    {
        var go = new GameObject("Hero");
        var health = go.AddComponent<Health>();
        SetFloat(health, "maxHp", 100f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().radius = 0.3f;
        var body = SpriteObject("Body", go.transform, circle, new Color(1f, 0.92f, 0.35f), Vector2.zero, Vector2.one * 0.6f, 20);

        var combat = go.AddComponent<HeroCombat>();
        SetArray(combat, "weapons", data.weapons);
        Set(combat, "arrowPrefab", arrow);
        Set(combat, "slashEffect", slash);
        Set(combat, "thrustEffect", thrust);

        var hero = go.AddComponent<Hero>();
        Set(hero, "body", body);
        return SavePrefab(go, PrefabFolder + "/Hero.prefab").GetComponent<Hero>();
    }

    // ------------------------------------------------------------------ scene

    static readonly Vector2 GateEnd = new Vector2(0f, -4.4f);
    static readonly Vector2[][] LanePoints =
    {
        new[] { new Vector2(-7f, 6.6f), new Vector2(-7f, 1f), new Vector2(-3f, -2.5f), GateEnd },
        new[] { new Vector2(0f, 6.6f), GateEnd },
        new[] { new Vector2(7f, 6.6f), new Vector2(7f, 1f), new Vector2(3f, -2.5f), GateEnd },
    };
    static readonly float[] SpotFractions = { 0.2f, 0.45f, 0.7f };

    static void CreateScene(DataSet data, PrefabSet prefabs)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera and light
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.1f, 0.08f);
        camGo.AddComponent<AudioListener>();
        var shake = camGo.AddComponent<CameraShake>();

        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        // Environment
        var environment = new GameObject("Environment").transform;
        SpriteObject("Background", environment, square, new Color(0.16f, 0.27f, 0.18f), Vector2.zero, new Vector2(24f, 14f), -100);

        var lanesRoot = new GameObject("Lanes").transform;
        var lanes = new LanePath[3];
        string[] laneNames = { "Left", "Middle", "Right" };
        for (int i = 0; i < 3; i++) lanes[i] = LaneObject(lanesRoot, i, laneNames[i]);

        var spotsRoot = new GameObject("Build Spots").transform;
        var spots = new List<BuildSpot>();
        for (int lane = 0; lane < 3; lane++)
            for (int slot = 0; slot < 3; slot++)
                spots.Add(SpotObject(spotsRoot, lane, slot));

        var gate = GateObject();

        // InstantiatePrefab wants the prefab's root GameObject, not one of its components.
        var heroGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.hero.gameObject);
        heroGo.transform.position = new Vector2(0f, -3f);
        var hero = heroGo.GetComponent<Hero>();

        var towersRoot = new GameObject("Towers").transform;
        var enemiesRoot = new GameObject("Enemies").transform;

        // Systems
        var game = new GameObject("Game");
        var manager = game.AddComponent<GameManager>();
        var build = game.AddComponent<BuildManager>();
        var spawner = game.AddComponent<WaveSpawner>();
        var bots = game.AddComponent<BotDirector>();

        Set(build, "config", data.config);
        SetArray(build, "spots", spots.ToArray());
        SetArray(build, "towerPrefabs", prefabs.towers);
        Set(build, "towerParent", towersRoot);

        Set(spawner, "config", data.config);
        SetArray(spawner, "enemyPrefabs", prefabs.enemies);
        SetArray(spawner, "lanes", lanes);
        Set(spawner, "hero", hero);
        Set(spawner, "gate", gate);
        Set(spawner, "enemyParent", enemiesRoot);

        Set(manager, "config", data.config);
        Set(manager, "hero", hero);
        Set(manager, "gate", gate);
        Set(manager, "build", build);
        Set(manager, "spawner", spawner);
        Set(manager, "cameraShake", shake);

        Set(bots, "game", manager);
        SetBotPersonas(bots);

        CreateUI();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AddSceneToBuild(ScenePath);
    }

    static LanePath LaneObject(Transform parent, int index, string name)
    {
        var go = new GameObject("Lane " + name);
        go.transform.SetParent(parent, false);
        var points = LanePoints[index];

        var waypoints = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var wp = new GameObject("Waypoint " + i).transform;
            wp.SetParent(go.transform, false);
            wp.position = points[i];
            waypoints[i] = wp;
        }

        var path = new GameObject("Path").transform;
        path.SetParent(go.transform, false);
        for (int i = 1; i < points.Length; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            var seg = SpriteObject("Segment " + i, path, square, new Color(0.55f, 0.47f, 0.33f), (a + b) * 0.5f,
                new Vector2(Vector2.Distance(a, b) + 1.1f, 1.1f), -50);
            seg.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        }

        var heat = SpriteObject("Heat", go.transform, circle, new Color(1f, 0.2f, 0.1f, 0f), PointAt(points, 0.35f, out _), new Vector2(2.6f, 2.6f), -45);

        var lane = go.AddComponent<LanePath>();
        SetInt(lane, "laneIndex", index);
        SetArray(lane, "waypoints", waypoints);
        Set(lane, "heatOverlay", heat);
        return lane;
    }

    static BuildSpot SpotObject(Transform parent, int lane, int slot)
    {
        var point = PointAt(LanePoints[lane], SpotFractions[slot], out var tangent);
        var normal = new Vector2(-tangent.y, tangent.x);
        float side = slot % 2 == 0 ? 1f : -1f;
        if (lane == 2) side = -side;
        int index = lane * 3 + slot;

        var go = new GameObject("Build Spot " + index);
        go.transform.SetParent(parent, false);
        go.transform.position = point + normal * 1.3f * side;
        go.AddComponent<CircleCollider2D>().radius = 0.45f;
        var marker = SpriteObject("Marker", go.transform, square, new Color(1f, 1f, 1f, 0.18f), Vector2.zero, Vector2.one * 0.9f, -40);
        var spot = go.AddComponent<BuildSpot>();
        SetInt(spot, "index", index);
        Set(spot, "marker", marker);
        return spot;
    }

    static Gate GateObject()
    {
        var go = new GameObject("Gate");
        go.transform.position = new Vector2(0f, -5.1f);
        var health = go.AddComponent<Health>();
        SetFloat(health, "maxHp", 500f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(3.2f, 0.9f);
        var body = SpriteObject("Body", go.transform, square, new Color(0.6f, 0.42f, 0.25f), Vector2.zero, new Vector2(3.2f, 0.9f), -30);
        var attackPoint = new GameObject("AttackPoint").transform;
        attackPoint.SetParent(go.transform, false);
        attackPoint.position = GateEnd;
        var gate = go.AddComponent<Gate>();
        Set(gate, "attackPoint", attackPoint);
        Set(gate, "body", body);
        return gate;
    }

    static void SetBotPersonas(BotDirector bots)
    {
        var so = new SerializedObject(bots);
        var list = so.FindProperty("personas");
        var personas = new[]
        {
            ("Cannon Turtle", WeaponType.Sword, 0, new[] { (0, TowerType.Cannon), (1, TowerType.Cannon), (2, TowerType.Cannon), (3, TowerType.Frost), (4, TowerType.Frost), (5, TowerType.Frost) }),
            ("Archer Line", WeaponType.Bow, 1, new[] { (3, TowerType.Archer), (4, TowerType.Archer), (5, TowerType.Archer), (6, TowerType.Cannon), (7, TowerType.Cannon), (8, TowerType.Cannon) }),
            ("Spread", WeaponType.Spear, 2, new[] { (0, TowerType.Archer), (4, TowerType.Cannon), (8, TowerType.Frost), (7, TowerType.Archer), (1, TowerType.Cannon), (5, TowerType.Frost) }),
        };
        list.arraySize = personas.Length;
        for (int i = 0; i < personas.Length; i++)
        {
            var (name, weapon, lane, placements) = personas[i];
            var p = list.GetArrayElementAtIndex(i);
            p.FindPropertyRelative("name").stringValue = name;
            p.FindPropertyRelative("weapon").enumValueIndex = (int)weapon;
            p.FindPropertyRelative("favouriteLane").intValue = lane;
            p.FindPropertyRelative("noise").floatValue = 0.1f;
            var pl = p.FindPropertyRelative("placements");
            pl.arraySize = placements.Length;
            for (int j = 0; j < placements.Length; j++)
            {
                var e = pl.GetArrayElementAtIndex(j);
                e.FindPropertyRelative("spot").intValue = placements[j].Item1;
                e.FindPropertyRelative("tower").enumValueIndex = (int)placements[j].Item2;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // --------------------------------------------------------------------- UI

    static void CreateUI()
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();

        var ui = canvasGo.AddComponent<UIManager>();
        Set(ui, "hud", HudPanel(root));
        Set(ui, "buildPanel", BuildPanel(root));
        Set(ui, "weaponPanel", WeaponPanel(root));
        Set(ui, "intentBanner", IntentPanel(root));
        Set(ui, "reportPanel", ReportPanel(root));
        Set(ui, "gameOverPanel", GameOverPanel(root));
        Set(ui, "brainPanel", BrainPanel(root));
    }

    static readonly Vector2 TopLeft = new Vector2(0f, 1f);
    static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
    static readonly Vector2 TopRight = new Vector2(1f, 1f);
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

    static HudView HudPanel(Transform root)
    {
        var hud = RectObject("HUD", root, Center, Center, Vector2.zero, Vector2.zero);
        hud.anchorMin = Vector2.zero;
        hud.anchorMax = Vector2.one;
        hud.offsetMin = hud.offsetMax = Vector2.zero;

        var scorePanel = Panel("Score Panel", hud, TopLeft, TopLeft, new Vector2(20f, -20f), new Vector2(440f, 110f));
        var score = Label("Score", scorePanel, TopLeft, TopLeft, new Vector2(20f, -10f), new Vector2(400f, 50f), 34, TextAlignmentOptions.Left, "Score 0    Wave 1");
        score.fontStyle = FontStyles.Bold;
        var stats = Label("Stats", scorePanel, TopLeft, TopLeft, new Vector2(20f, -60f), new Vector2(400f, 40f), 24, TextAlignmentOptions.Left, "Kills 0    Survived 0");

        var gateBar = Bar("Gate Bar", hud, new Vector2(0f, -20f), new Vector2(600f, 36f), new Color(0.85f, 0.6f, 0.3f), out var gateText);
        var heroBar = Bar("Hero Bar", hud, new Vector2(0f, -62f), new Vector2(600f, 28f), new Color(0.35f, 0.85f, 0.4f), out var heroText);

        var phasePanel = Panel("Phase Panel", hud, TopRight, TopRight, new Vector2(-20f, -20f), new Vector2(440f, 200f));
        var phase = Label("Phase", phasePanel, TopLeft, TopLeft, new Vector2(20f, -10f), new Vector2(400f, 46f), 32, TextAlignmentOptions.Left, "Build your defence");
        phase.fontStyle = FontStyles.Bold;
        var info = Label("Info", phasePanel, TopLeft, TopLeft, new Vector2(20f, -58f), new Vector2(400f, 130f), 24, TextAlignmentOptions.TopLeft, "");

        var view = hud.gameObject.AddComponent<HudView>();
        Set(view, "scoreText", score);
        Set(view, "statsText", stats);
        Set(view, "gateFill", gateBar);
        Set(view, "gateText", gateText);
        Set(view, "heroFill", heroBar);
        Set(view, "heroText", heroText);
        Set(view, "phaseText", phase);
        Set(view, "infoText", info);
        return view;
    }

    static BuildPanelView BuildPanel(Transform root)
    {
        var panel = Panel("Build Panel", root, BottomCenter, BottomCenter, new Vector2(0f, 20f), new Vector2(1280f, 150f));
        var buttons = new Button[3];
        var labels = new TMP_Text[3];
        string[] names = { "Archer", "Cannon", "Frost" };
        for (int i = 0; i < 3; i++)
            buttons[i] = ButtonAt(names[i] + " Button", panel, new Vector2(20f + i * 345f, -20f), new Vector2(330f, 110f), names[i], 24, out labels[i]);
        var count = Label("Count", panel, TopLeft, TopLeft, new Vector2(1060f, -12f), new Vector2(205f, 72f), 26, TextAlignmentOptions.TopLeft, "Defences 0/6");
        var ready = ButtonAt("Ready Button", panel, new Vector2(1060f, -88f), new Vector2(200f, 48f), "<b>Ready</b> [Enter]", 22, out _);

        var view = panel.gameObject.AddComponent<BuildPanelView>();
        SetArray(view, "towerButtons", buttons);
        SetArray(view, "towerLabels", labels);
        Set(view, "countText", count);
        Set(view, "readyButton", ready);
        panel.gameObject.SetActive(false);
        return view;
    }

    static WeaponPanelView WeaponPanel(Transform root)
    {
        var panel = Panel("Weapon Panel", root, Center, Center, Vector2.zero, new Vector2(1120f, 300f));
        Label("Title", panel, TopLeft, TopLeft, new Vector2(20f, -15f), new Vector2(1080f, 50f), 32, TextAlignmentOptions.Center, "Choose your weapon for this wave");
        var buttons = new Button[3];
        var labels = new TMP_Text[3];
        string[] names = { "Bow", "Sword", "Spear" };
        for (int i = 0; i < 3; i++)
            buttons[i] = ButtonAt(names[i] + " Button", panel, new Vector2(20f + i * 365f, -85f), new Vector2(350f, 190f), names[i], 26, out labels[i]);

        var view = panel.gameObject.AddComponent<WeaponPanelView>();
        SetArray(view, "weaponButtons", buttons);
        SetArray(view, "weaponLabels", labels);
        panel.gameObject.SetActive(false);
        return view;
    }

    static IntentBannerView IntentPanel(Transform root)
    {
        var panel = Panel("Intent Banner", root, TopCenter, TopCenter, new Vector2(0f, -120f), new Vector2(1040f, 130f));
        var text = Label("Text", panel, Center, Center, Vector2.zero, Vector2.zero, 26, TextAlignmentOptions.Left, "");
        Stretch(text.rectTransform, 20f);
        var view = panel.gameObject.AddComponent<IntentBannerView>();
        Set(view, "text", text);
        panel.gameObject.SetActive(false);
        return view;
    }

    static ReportView ReportPanel(Transform root)
    {
        var panel = Panel("Report Panel", root, Center, Center, Vector2.zero, new Vector2(1040f, 580f));
        var title = Label("Title", panel, TopLeft, TopLeft, new Vector2(24f, -18f), new Vector2(990f, 50f), 34, TextAlignmentOptions.Left, "War report");
        title.fontStyle = FontStyles.Bold;
        var body = Label("Body", panel, TopLeft, TopLeft, new Vector2(24f, -78f), new Vector2(990f, 400f), 24, TextAlignmentOptions.TopLeft, "");
        var button = ButtonAt("Continue Button", panel, Vector2.zero, new Vector2(360f, 60f), "<b>Next wave</b> [Enter]", 26, out _);
        Anchor(button.GetComponent<RectTransform>(), BottomCenter, BottomCenter, new Vector2(0f, 20f));

        var view = panel.gameObject.AddComponent<ReportView>();
        Set(view, "titleText", title);
        Set(view, "bodyText", body);
        Set(view, "continueButton", button);
        panel.gameObject.SetActive(false);
        return view;
    }

    static GameOverView GameOverPanel(Transform root)
    {
        var panel = Panel("Game Over Panel", root, Center, Center, Vector2.zero, new Vector2(1040f, 680f));
        var title = Label("Title", panel, TopCenter, TopCenter, new Vector2(0f, -20f), new Vector2(1000f, 90f), 64, TextAlignmentOptions.Center, "Game Over");
        title.fontStyle = FontStyles.Bold;
        var summary = Label("Summary", panel, TopLeft, TopLeft, new Vector2(24f, -120f), new Vector2(990f, 100f), 28, TextAlignmentOptions.Center, "");
        var report = Label("Report", panel, TopLeft, TopLeft, new Vector2(24f, -230f), new Vector2(990f, 330f), 22, TextAlignmentOptions.TopLeft, "");
        var button = ButtonAt("Restart Button", panel, Vector2.zero, new Vector2(360f, 60f), "<b>Restart</b> [R]", 26, out _);
        Anchor(button.GetComponent<RectTransform>(), BottomCenter, BottomCenter, new Vector2(0f, 20f));

        var view = panel.gameObject.AddComponent<GameOverView>();
        Set(view, "summaryText", summary);
        Set(view, "reportText", report);
        Set(view, "restartButton", button);
        panel.gameObject.SetActive(false);
        return view;
    }

    static BrainPanelView BrainPanel(Transform root)
    {
        var panel = Panel("Brain Panel", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, -40f), new Vector2(640f, 780f));
        var title = Label("Title", panel, TopLeft, TopLeft, new Vector2(20f, -15f), new Vector2(600f, 46f), 30, TextAlignmentOptions.Left, "AI brain");
        title.fontStyle = FontStyles.Bold;
        var info = Label("Info", panel, TopLeft, TopLeft, new Vector2(20f, -62f), new Vector2(600f, 60f), 18, TextAlignmentOptions.TopLeft, "");

        var rows = new BrainRow[16];
        for (int a = 0; a < rows.Length; a++)
        {
            var row = RectObject("Row " + a, panel, TopLeft, TopLeft, new Vector2(20f, -130f - a * 39f), new Vector2(600f, 36f));
            var label = Label("Label", row, TopLeft, TopLeft, Vector2.zero, new Vector2(270f, 36f), 18, TextAlignmentOptions.Left, "");
            var area = Panel("Bar Area", row, TopLeft, TopLeft, new Vector2(275f, -6f), new Vector2(325f, 24f));
            area.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            var mean = Panel("Mean", area, TopLeft, TopLeft, Vector2.zero, Vector2.zero).GetComponent<Image>();
            var bonus = Panel("Bonus", area, TopLeft, TopLeft, Vector2.zero, Vector2.zero).GetComponent<Image>();
            rows[a] = row.gameObject.AddComponent<BrainRow>();
            Set(rows[a], "label", label);
            Set(rows[a], "meanBar", mean);
            Set(rows[a], "bonusBar", bonus);
        }

        var view = panel.gameObject.AddComponent<BrainPanelView>();
        Set(view, "titleText", title);
        Set(view, "infoText", info);
        SetArray(view, "rows", rows);
        panel.gameObject.SetActive(false);
        return view;
    }

    // ---------------------------------------------------------- UI helpers

    static RectTransform RectObject(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
    }

    static void Stretch(RectTransform rt, float padding)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rt = RectObject(name, parent, anchor, pivot, position, size);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = square;
        image.color = PanelColor;
        return rt;
    }

    static TMP_Text Label(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
        float fontSize, TextAlignmentOptions alignment, string text)
    {
        var rt = RectObject(name, parent, anchor, pivot, position, size);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = TMP_Settings.defaultFontAsset;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = Color.white;
        t.richText = true;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    static Button ButtonAt(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize, out TMP_Text label)
    {
        var rt = Panel(name, parent, TopLeft, TopLeft, position, size);
        var image = rt.GetComponent<Image>();
        image.color = NormalButton;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        label = Label("Label", rt, Center, Center, Vector2.zero, Vector2.zero, fontSize, TextAlignmentOptions.Center, text);
        Stretch(label.rectTransform, 10f);
        return button;
    }

    static Image Bar(string name, Transform parent, Vector2 position, Vector2 size, Color fillColor, out TMP_Text label)
    {
        var back = Panel(name, parent, TopCenter, TopCenter, position, size);
        back.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        var fillRt = Panel("Fill", back, Center, Center, Vector2.zero, Vector2.zero);
        Stretch(fillRt, 3f);
        var fill = fillRt.GetComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        label = Label("Label", back, Center, Center, Vector2.zero, Vector2.zero, 22, TextAlignmentOptions.Center, "");
        Stretch(label.rectTransform, 0f);
        return fill;
    }

    // ------------------------------------------------------- general helpers

    static SpriteRenderer SpriteObject(string name, Transform parent, Sprite sprite, Color color, Vector2 localPosition, Vector2 scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static Vector2 PointAt(Vector2[] points, float t, out Vector2 tangent)
    {
        float total = 0f;
        for (int i = 1; i < points.Length; i++) total += Vector2.Distance(points[i - 1], points[i]);
        float target = t * total;
        for (int i = 1; i < points.Length; i++)
        {
            float segment = Vector2.Distance(points[i - 1], points[i]);
            if (target <= segment)
            {
                tangent = (points[i] - points[i - 1]).normalized;
                return Vector2.Lerp(points[i - 1], points[i], target / segment);
            }
            target -= segment;
        }
        tangent = (points[points.Length - 1] - points[points.Length - 2]).normalized;
        return points[points.Length - 1];
    }

    static GameObject SavePrefab(GameObject go, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void AddSceneToBuild(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAll(s => s.path == path);
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static SerializedProperty Property(SerializedObject so, string field)
    {
        var p = so.FindProperty(field);
        if (p == null) Debug.LogError("[Gatekeeper setup] No field '" + field + "' on " + so.targetObject.GetType().Name);
        return p;
    }

    static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = Property(so, field);
        if (p == null) return;
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetArray<T>(Object target, string field, T[] values) where T : Object
    {
        var so = new SerializedObject(target);
        var p = Property(so, field);
        if (p == null) return;
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        var p = Property(so, field);
        if (p == null) return;
        p.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetInt(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        var p = Property(so, field);
        if (p == null) return;
        p.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
