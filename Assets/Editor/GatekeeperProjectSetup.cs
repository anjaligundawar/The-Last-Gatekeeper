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
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// One-time setup: Gatekeeper > Create Project Assets.
/// Uses the pixel art in Assets/Art/Pixel (plain shapes for the hero and enemies until their
/// sprites arrive) and creates tiles, data assets, prefabs and the Gatekeeper scene, all wired up.
/// After it has run, everything is ordinary project assets you edit in the Inspector.
/// You can delete this script afterwards.
/// </summary>
public static class GatekeeperProjectSetup
{
    const string ArtFolder = "Assets/Art/Shapes";
    const string PixelFolder = "Assets/Art/Pixel";
    const string TileFolder = "Assets/Art/Tiles";
    const string FontPath = "Assets/Art/Fonts/PixelifySans.ttf";
    const string FontAssetPath = "Assets/Art/Fonts/PixelifySans SDF.asset";
    const string FlashMaterialPath = "Assets/Art/Materials/SpriteFlash.mat";
    const string DataFolder = "Assets/Data";
    const string PrefabFolder = "Assets/Prefabs";
    const string ScenePath = "Assets/Scenes/Gatekeeper.unity";

    static Sprite square, circle, diamond, triangle, ring;
    static Material flashMaterial;
    static TMP_FontAsset uiFont;

    // Palette colours (see game-design/art-style/art-style-guide.md)
    static readonly Color Ink = Hex("#1A2B29");
    static readonly Color HeroGreen = Hex("#99CB6A");
    static readonly Color GateOrange = Hex("#ECA94B");
    static readonly Color FrostCyan = Hex("#6EF0F7");

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

        foreach (var folder in new[] { ArtFolder, TileFolder, "Assets/Art/Materials", DataFolder + "/Enemies", DataFolder + "/Towers", DataFolder + "/Weapons",
                     PrefabFolder + "/Enemies", PrefabFolder + "/Towers", PrefabFolder + "/Projectiles", PrefabFolder + "/Effects", "Assets/Scenes" })
            EnsureFolder(folder);

        // Open the new scene first: opening a scene unloads unused assets, which would
        // destroy the prefabs and data assets created below before they are wired up.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateSprites();
        PrepareArt();
        var data = CreateData();
        CreatePrefabs(data);
        AssetDatabase.SaveAssets();
        CreateScene(data, LoadPrefabs(data));

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

    // ------------------------------------------------------------- pixel art

    // Reimports the pixel art so PixelArtImporter's settings are applied, then
    // builds the flash material and the UI font asset.
    static void PrepareArt()
    {
        AssetDatabase.ImportAsset(PixelFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

        flashMaterial = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
        var shader = Shader.Find("Gatekeeper/Sprite Flash");
        if (flashMaterial == null && shader != null)
        {
            flashMaterial = new Material(shader);
            AssetDatabase.CreateAsset(flashMaterial, FlashMaterialPath);
        }
        if (shader == null) Debug.LogWarning("[Gatekeeper setup] Sprite Flash shader not found; enemies will flash by tint instead.");

        uiFont = CreateFontAsset();
    }

    static TMP_FontAsset CreateFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null) return null;
        try
        {
            var asset = TMP_FontAsset.CreateFontAsset(font, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            asset.name = "PixelifySans SDF";
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            asset.atlasTexture.name = "PixelifySans Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            asset.material.name = "PixelifySans Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Gatekeeper setup] Could not create the pixel font asset, using the default font. " + e.Message);
            return null;
        }
    }

    static Sprite Art(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PixelFolder + "/" + path + ".png");
        if (sprite == null) Debug.LogError("[Gatekeeper setup] Missing sprite " + PixelFolder + "/" + path + ".png");
        return sprite;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
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
            Asset<EnemyData>(DataFolder + "/Enemies/Shield.asset", e => { FillEnemy(e, EnemyType.Shield, "Shield", 60, 1f, 6, 1.2f, 2, false, 0.25f, 1.25f); e.knockbackScale = 0.4f; }),
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
        e.knockbackScale = 1f;
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
        // Enemy kingdom violets plus one warm accent, until the character sprites arrive
        Hex("#9A6FC0"), Hex("#D3A8F0"), Hex("#6B4E8C"), Hex("#E07045"),
    };
    static readonly float[] EnemySizes = { 0.45f, 0.35f, 0.55f, 0.4f };

    static PrefabSet CreatePrefabs(DataSet data)
    {
        var deathFx = Effect("Fx_Death", Art("Effects/Death"), new Color(1f, 1f, 1f, 0.9f), 0.2f);
        var explosionFx = Effect("Fx_Explosion", Art("Effects/Explosion"), new Color(1f, 1f, 1f, 0.9f), 0.2f);
        var slashFx = Effect("Fx_SwordSlash", Art("Effects/Slash"), new Color(1f, 1f, 1f, 0.7f), 0.12f);
        var thrustFx = Effect("Fx_SpearThrust", Art("Effects/Thrust"), new Color(1f, 1f, 1f, 0.6f), 0.1f);
        var frostFx = Effect("Fx_FrostPulse", ring, new Color(FrostCyan.r, FrostCyan.g, FrostCyan.b, 0.6f), 0.3f);

        var arrow = ProjectilePrefab("Arrow", Art("Projectiles/Arrow"), 12f, 1.5f, null);
        var cannonball = ProjectilePrefab("Cannonball", Art("Projectiles/Cannonball"), 7f, 3f, explosionFx);

        var set = new PrefabSet { enemies = new Enemy[4], towers = new Tower[3] };
        Sprite[] enemyShapes = { square, triangle, square, circle };
        for (int i = 0; i < 4; i++)
            set.enemies[i] = EnemyPrefab(data.enemies[i], enemyShapes[i], EnemyColors[i], EnemySizes[i], deathFx);

        Sprite[] towerShapes = { Art("Towers/Archer"), Art("Towers/Cannon"), Art("Towers/Frost") };
        Projectile[] towerShots = { arrow, cannonball, null };
        for (int i = 0; i < 3; i++)
            set.towers[i] = TowerPrefab(data.towers[i], towerShapes[i], towerShots[i], i == (int)TowerType.Frost ? frostFx : null);

        set.hero = HeroPrefab(data, arrow, slashFx, thrustFx);
        return set;
    }

    // Reload the prefabs from disk so the scene is wired to the saved assets, not to
    // component references that the editor may have unloaded since they were created.
    static PrefabSet LoadPrefabs(DataSet data)
    {
        var set = new PrefabSet { enemies = new Enemy[data.enemies.Length], towers = new Tower[data.towers.Length] };
        set.hero = LoadPrefab<Hero>(PrefabFolder + "/Hero.prefab");
        for (int i = 0; i < set.enemies.Length; i++)
            set.enemies[i] = LoadPrefab<Enemy>(PrefabFolder + "/Enemies/" + data.enemies[i].displayName + ".prefab");
        for (int i = 0; i < set.towers.Length; i++)
            set.towers[i] = LoadPrefab<Tower>(PrefabFolder + "/Towers/" + data.towers[i].displayName + ".prefab");
        return set;
    }

    static T LoadPrefab<T>(string path) where T : Component
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var component = go != null ? go.GetComponent<T>() : null;
        if (component == null)
            throw new System.InvalidOperationException("[Gatekeeper setup] Could not load " + typeof(T).Name + " from " + path);
        return component;
    }

    static FadeOut Effect(string name, Sprite sprite, Color color, float duration)
    {
        var go = new GameObject(name);
        // Scale so the sprite covers 1 x 1 unit; FadeOut.Spawn then sizes it in world units.
        var bounds = sprite.bounds.size;
        go.transform.localScale = new Vector3(1f / bounds.x, 1f / bounds.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 35;
        var fx = go.AddComponent<FadeOut>();
        SetFloat(fx, "duration", duration);
        return SavePrefab(go, PrefabFolder + "/Effects/" + name + ".prefab").GetComponent<FadeOut>();
    }

    static Projectile ProjectilePrefab(string name, Sprite sprite, float speed, float knockback, FadeOut impact)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 30;
        var projectile = go.AddComponent<Projectile>();
        SetFloat(projectile, "speed", speed);
        SetFloat(projectile, "knockback", knockback);
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

        SpriteObject("Shadow", go.transform, Art("Effects/Shadow"), Color.white, new Vector2(0f, data.flying ? -0.45f : -size * 0.45f), Vector2.one, 9);
        var body = SpriteObject("Body", go.transform, shape, color, Vector2.zero, Vector2.one * size, data.flying ? 15 : 10);
        if (flashMaterial != null) body.sharedMaterial = flashMaterial;
        var icon = SpriteObject("StateIcon", go.transform, Art("Icons/State_Chase"), Color.white, new Vector2(0f, size * 0.5f + 0.45f), Vector2.one, 40);

        var barRoot = new GameObject("HealthBar");
        barRoot.transform.SetParent(go.transform, false);
        barRoot.transform.localPosition = new Vector2(0f, size * 0.5f + 0.15f);
        var back = SpriteObject("Background", barRoot.transform, square, Ink, Vector2.zero, new Vector2(0.6f, 0.1f), 41);
        var fill = SpriteObject("Fill", back.transform, square, HeroGreen, Vector2.zero, Vector2.one, 42);
        var bar = barRoot.AddComponent<HealthBar>();
        Set(bar, "health", health);
        Set(bar, "fill", fill.transform);

        var enemy = go.AddComponent<Enemy>();
        Set(enemy, "data", data);
        Set(enemy, "body", body);
        Set(enemy, "stateIcon", icon);
        Set(enemy, "deathEffect", deathFx);
        Set(enemy, "chaseIcon", Art("Icons/State_Chase"));
        Set(enemy, "attackIcon", Art("Icons/State_Attack"));
        Set(enemy, "attackGateIcon", Art("Icons/State_AttackGate"));
        return SavePrefab(go, PrefabFolder + "/Enemies/" + data.displayName + ".prefab").GetComponent<Enemy>();
    }

    static Tower TowerPrefab(TowerData data, Sprite shape, Projectile projectile, FadeOut pulse)
    {
        var go = new GameObject(data.displayName + " Tower");
        // The tower art's pivot is at its base, so it stands on the build spot.
        SpriteObject("Body", go.transform, shape, Color.white, new Vector2(0f, -0.45f), Vector2.one, 5);
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

        SpriteObject("Shadow", go.transform, Art("Effects/Shadow"), Color.white, new Vector2(0f, -0.3f), Vector2.one, 19);

        // Weapon on a pivot at the hero's centre; HeroWeaponVisual rotates the pivot.
        var pivot = new GameObject("WeaponPivot").transform;
        pivot.SetParent(go.transform, false);
        var weapon = SpriteObject("Weapon", pivot, Art("Weapons/Bow"), Color.white, new Vector2(0.35f, 0f), Vector2.one, 21);
        var visual = go.AddComponent<HeroWeaponVisual>();
        Set(visual, "pivot", pivot);
        Set(visual, "weaponRenderer", weapon);
        SetArray(visual, "weaponSprites", new[] { Art("Weapons/Bow"), Art("Weapons/Sword"), Art("Weapons/Spear") });

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
        // Camera and light
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.75f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("#28493B");
        camGo.AddComponent<AudioListener>();
        // 384 x 216 art pixels = 24 x 13.5 tiles, scaled up 5x on a 1080p screen.
        var pixelPerfect = camGo.AddComponent<PixelPerfectCamera>();
        pixelPerfect.assetsPPU = PixelArtImporter.PixelsPerUnit;
        pixelPerfect.refResolutionX = 384;
        pixelPerfect.refResolutionY = 216;
        var shake = camGo.AddComponent<CameraShake>();

        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        // Environment
        var environment = new GameObject("Environment").transform;
        PaintGround(environment);

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
        PlaceWalls(environment);
        PlaceDecor(environment, spots);

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

        // The lane itself is painted on the Ground tilemap (see PaintGround).

        var lane = go.AddComponent<LanePath>();
        SetInt(lane, "laneIndex", index);
        SetArray(lane, "waypoints", waypoints);
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
        var marker = SpriteObject("Marker", go.transform, Art("Map/BuildSpot"), Color.white, Vector2.zero, Vector2.one, -40);
        var spot = go.AddComponent<BuildSpot>();
        SetInt(spot, "index", index);
        Set(spot, "marker", marker);
        Set(spot, "idleSprite", Art("Map/BuildSpot"));
        Set(spot, "hoverSprite", Art("Map/BuildSpot_Hover"));
        return spot;
    }

    static Gate GateObject()
    {
        var go = new GameObject("Gate");
        go.transform.position = new Vector2(0f, -5.1f);
        var health = go.AddComponent<Health>();
        SetFloat(health, "maxHp", 500f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(3.2f, 0.9f);
        // Pivot at the gate's base, which sits on the bottom edge of the map.
        var body = SpriteObject("Body", go.transform, Art("Map/Gate"), Color.white, new Vector2(0f, MapBottom - go.transform.position.y), Vector2.one, 4);
        var attackPoint = new GameObject("AttackPoint").transform;
        attackPoint.SetParent(go.transform, false);
        attackPoint.position = GateEnd;
        var gate = go.AddComponent<Gate>();
        Set(gate, "attackPoint", attackPoint);
        Set(gate, "body", body);
        Set(gate, "damagedSprite", Art("Map/Gate_Damaged"));
        Set(gate, "brokenSprite", Art("Map/Gate_Broken"));
        return gate;
    }

    // ------------------------------------------------------------ map art

    // The play area is 24 x 14 tiles, from (-12, -7) to (12, 7). One extra ring of
    // tiles is painted off-screen so the lanes run off the edge instead of ending.
    const int MapMinX = -12, MapMaxX = 12, MapMinY = -7, MapMaxY = 7;
    const float MapBottom = -7f;
    const float PathHalfWidth = 1.05f;

    static void PaintGround(Transform parent)
    {
        var grassTiles = new TileBase[4];
        for (int i = 0; i < grassTiles.Length; i++)
        {
            var sprite = Art("Tiles/Grass_" + i);
            grassTiles[i] = Asset<Tile>(TileFolder + "/Grass_" + i + ".asset", t => { t.sprite = sprite; t.colliderType = Tile.ColliderType.None; });
        }
        var pathTile = PathRuleTile();

        var grid = new GameObject("Grid", typeof(Grid));
        grid.transform.SetParent(parent, false);
        var ground = new GameObject("Ground");
        ground.transform.SetParent(grid.transform, false);
        var tilemap = ground.AddComponent<Tilemap>();
        ground.AddComponent<TilemapRenderer>().sortingOrder = -100;

        for (int y = MapMinY - 1; y <= MapMaxY; y++)
            for (int x = MapMinX - 1; x <= MapMaxX; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (IsPath(x, y)) tilemap.SetTile(cell, pathTile);
                else
                {
                    int h = Hash(x, y);
                    tilemap.SetTile(cell, grassTiles[h % 3 == 0 ? h % 4 : 0]);
                }
            }
    }

    // A Rule Tile with one rule per combination of path neighbours (up, right, down, left),
    // so lanes painted with it join up on their own.
    static TileBase PathRuleTile()
    {
        var sprites = new Sprite[16];
        for (int m = 0; m < 16; m++) sprites[m] = Art("Tiles/Path_" + m.ToString("00"));
        return Asset<RuleTile>(TileFolder + "/Path.asset", tile =>
        {
            tile.m_DefaultSprite = sprites[15];
            tile.m_DefaultColliderType = Tile.ColliderType.None;
            tile.m_TilingRules = new List<RuleTile.TilingRule>();
            var directions = new[] { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };
            for (int m = 0; m < 16; m++)
            {
                var rule = new RuleTile.TilingRule
                {
                    m_Sprites = new[] { sprites[m] },
                    m_ColliderType = Tile.ColliderType.None,
                    m_NeighborPositions = new List<Vector3Int>(directions),
                    m_Neighbors = new List<int>(),
                };
                for (int bit = 0; bit < 4; bit++)
                    rule.m_Neighbors.Add((m & (1 << bit)) != 0
                        ? RuleTile.TilingRuleOutput.Neighbor.This
                        : RuleTile.TilingRuleOutput.Neighbor.NotThis);
                tile.m_TilingRules.Add(rule);
            }
        });
    }

    static bool IsPath(int x, int y) => DistanceToLanes(new Vector2(x + 0.5f, y + 0.5f)) <= PathHalfWidth;

    static float DistanceToLanes(Vector2 p)
    {
        float best = float.MaxValue;
        foreach (var lane in LanePoints)
            for (int i = 1; i < lane.Length; i++)
            {
                var a = lane[i - 1];
                var ab = lane[i] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
        return best;
    }

    static int Hash(int x, int y) => ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;

    static void PlaceWalls(Transform parent)
    {
        var root = new GameObject("Castle Wall").transform;
        root.SetParent(parent, false);
        var walls = new[] { Art("Map/Wall_0"), Art("Map/Wall_1") };
        for (int x = MapMinX; x < MapMaxX; x++)
        {
            if (x == -1 || x == 0) continue; // the gate stands here
            SpriteObject("Wall " + x, root, walls[(x % 2 + 2) % 2], Color.white, new Vector2(x + 0.5f, MapBottom), Vector2.one, 4);
        }
    }

    // Forest along the left, right and top edges, and a few props in the open grass.
    // Nothing is placed on or next to a lane, near a build spot, or by the wall.
    static void PlaceDecor(Transform parent, List<BuildSpot> spots)
    {
        var root = new GameObject("Decor").transform;
        root.SetParent(parent, false);
        Sprite[] roundTrees = { Art("Props/Tree_Round_0"), Art("Props/Tree_Round_1") };
        var pine = Art("Props/Tree_Pine");
        var bush = Art("Props/Bush");
        var rock = Art("Props/Rock");
        Sprite[] flowers = { Art("Props/Flowers_0"), Art("Props/Flowers_1") };

        for (int y = MapMinY + 2; y < MapMaxY; y++)
            for (int x = MapMinX; x < MapMaxX; x++)
            {
                var centre = new Vector2(x + 0.5f, y + 0.5f);
                if (DistanceToLanes(centre) <= 1.7f) continue;
                bool nearSpot = false;
                foreach (var spot in spots)
                    if (Vector2.Distance(centre, spot.transform.position) < 1.3f) nearSpot = true;
                if (nearSpot) continue;

                int h = Hash(x, y);
                bool border = x <= MapMinX + 1 || x >= MapMaxX - 2 || y >= MapMaxY - 1;
                Sprite sprite = null;
                if (border)
                {
                    if ((x + y) % 2 == 0) sprite = roundTrees[h % 2];
                    else if (h % 3 == 0) sprite = pine;
                }
                else if (h % 19 == 0) sprite = bush;
                else if (h % 23 == 1) sprite = rock;
                else if (h % 13 == 2) sprite = flowers[h % 2];

                if (sprite != null)
                    SpriteObject(sprite.name, root, sprite, Color.white, new Vector2(centre.x, y), Vector2.one, sprite == flowers[0] || sprite == flowers[1] ? -90 : 5);
            }
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

        var gateBar = Bar("Gate Bar", hud, new Vector2(0f, -20f), new Vector2(600f, 40f), GateOrange, out var gateText);
        var heroBar = Bar("Hero Bar", hud, new Vector2(0f, -66f), new Vector2(600f, 32f), HeroGreen, out var heroText);
        Icon("Gate Icon", gateBar.transform.parent, Art("Icons/Castle"), new Vector2(-34f, 0f), 50f, anchorLeft: true);
        Icon("Hero Icon", heroBar.transform.parent, Art("Icons/Heart"), new Vector2(-34f, 0f), 42f, anchorLeft: true);

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
        string[] towerArt = { "Towers/Archer", "Towers/Cannon", "Towers/Frost" };
        for (int i = 0; i < 3; i++)
        {
            buttons[i] = ButtonAt(names[i] + " Button", panel, new Vector2(20f + i * 345f, -20f), new Vector2(330f, 110f), names[i], 24, out labels[i]);
            Icon("Icon", buttons[i].transform, Art(towerArt[i]), new Vector2(50f, 0f), 90f, anchorLeft: true);
            labels[i].rectTransform.offsetMin = new Vector2(95f, labels[i].rectTransform.offsetMin.y);
        }
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
        string[] weaponArt = { "Weapons/Bow", "Weapons/Sword", "Weapons/Spear" };
        for (int i = 0; i < 3; i++)
        {
            buttons[i] = ButtonAt(names[i] + " Button", panel, new Vector2(20f + i * 365f, -85f), new Vector2(350f, 190f), names[i], 26, out labels[i]);
            var icon = Icon("Icon", buttons[i].transform, Art(weaponArt[i]), new Vector2(0f, 48f), 110f);
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            labels[i].rectTransform.offsetMax = new Vector2(labels[i].rectTransform.offsetMax.x, -90f);
        }

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
        Sliced(image, Art("UI/Panel"));
        return rt;
    }

    // Pixel-art 9-slice: each art pixel of the frame becomes 4 canvas pixels.
    static void Sliced(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 100f / PixelArtImporter.PixelsPerUnit / 4f;
        image.color = Color.white;
    }

    static Image Plain(Image image, Color color)
    {
        image.sprite = Art("UI/White");
        image.type = Image.Type.Simple;
        image.color = color;
        return image;
    }

    static Image Icon(string name, Transform parent, Sprite sprite, Vector2 position, float size, bool anchorLeft = false)
    {
        var anchor = anchorLeft ? new Vector2(0f, 0.5f) : Center;
        var rt = RectObject(name, parent, anchor, Center, position, Vector2.one * size);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Label(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
        float fontSize, TextAlignmentOptions alignment, string text)
    {
        var rt = RectObject(name, parent, anchor, pivot, position, size);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = uiFont != null ? uiFont : TMP_Settings.defaultFontAsset;
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
        Sliced(image, Art("UI/Button_Normal"));
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = Art("UI/Button_Hover"),
            selectedSprite = Art("UI/Button_Hover"),
            pressedSprite = Art("UI/Button_Pressed"),
            disabledSprite = Art("UI/Button_Pressed"),
        };
        label = Label("Label", rt, Center, Center, Vector2.zero, Vector2.zero, fontSize, TextAlignmentOptions.Center, text);
        Stretch(label.rectTransform, 10f);
        return button;
    }

    static Image Bar(string name, Transform parent, Vector2 position, Vector2 size, Color fillColor, out TMP_Text label)
    {
        var back = Panel(name, parent, TopCenter, TopCenter, position, size);
        Sliced(back.GetComponent<Image>(), Art("UI/Bar_Frame"));
        var fillRt = Panel("Fill", back, Center, Center, Vector2.zero, Vector2.zero);
        Stretch(fillRt, 10f);
        var fill = Plain(fillRt.GetComponent<Image>(), fillColor);
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
