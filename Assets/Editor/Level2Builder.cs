using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Narzedzie edytora generujace kompletny drugi poziom gry (Assets/Scenes/Level2.unity - Kamienna Twierdza),
/// nowy prefab fioletowego skorpiona, dodajace obie sceny do Build Settings
/// oraz podpinajace przycisk "Nastepny poziom" w SampleScene.unity.
/// </summary>
public static class Level2Builder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string Level2ScenePath = "Assets/Scenes/Level2.unity";
    private const string Level3ScenePath = "Assets/Scenes/Level3.unity";

    private const string CrystalPrefabPath = "Assets/Prefabs/Crystal.prefab";
    private const string CheckpointPrefabPath = "Assets/Prefabs/Checkpoint.prefab";
    private const string CratePrefabPath = "Assets/Prefabs/PushableCrate.prefab";
    private const string ScorpionPrefabPath = "Assets/Prefabs/Enemies/Scorpion.prefab";
    private const string SkeletonPrefabPath = "Assets/Prefabs/Enemies/Skeleton.prefab";
    private const string VioletScorpionPrefabPath = "Assets/Prefabs/Enemies/VioletScorpion.prefab";

    private const string StoneRuleTilePath = "Assets/SimpleRuleTiles/Stone/StoneRuleTile.asset";
    private const string StoneFallbackTilePath = "Assets/SimpleRuleTiles/Stone/StoneTileMap_1.asset";
    private const string PlayerPhysMatPath = "Assets/karsiori/TileMap/Material/physicmaterial.physicsMaterial2D";

    private const string CainosPropsPrefix = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - ";
    private const string CrystalSpritePath = "Assets/Sprites/CrystalGem.png";
    private const string PlayerOrbSpritePath = "Assets/Sprites/PlayerOrb.png";
    private const string GlowSpritePath = "Assets/Sprites/SoftGlow.png";
    private const string AudioClipPath = "Assets/Audio/853656__coghezzi__sacred-rune-lock-holy-magic-seal-activation.wav";

    private const string SkyBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_01_sky.png";
    private const string MountainsBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_02_mountains.png";
    private const string DungeonBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_04_dungeon_b.png";
    private const string CaveBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_05_cave_b.png";
    private const string ScorpionVioletPrefix = "Assets/Cave Platformer Tileset/Individual PNG files/Monster/scorpion-violet/scorpion-violet-";
    private const string MainFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private const int RequiredPoints = 18;

    [MenuItem("Tools/Build Level 2 (Kamienna Twierdza)")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        ConfigureTextures();
        Material spriteMat = ResolveSpriteMaterial();

        BuildVioletScorpionPrefab(spriteMat);

        Scene level2Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildLevel2SceneContents(spriteMat);
        EditorSceneManager.MarkSceneDirty(level2Scene);
        EditorSceneManager.SaveScene(level2Scene, Level2ScenePath);

        UpdateBuildSettings();
        ConnectSampleSceneToLevel2();

        EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();

        Debug.Log("[Level2Builder] Level 2 (Assets/Scenes/Level2.unity) oraz przejscie z SampleScene.unity wygenerowane pomyslnie!");
    }

    private static void ConfigureTextures()
    {
        ConfigureSingleSprite(CrystalSpritePath, 32f, FilterMode.Point);
        ConfigureSingleSprite(PlayerOrbSpritePath, 32f, FilterMode.Point);
        ConfigureSingleSprite(GlowSpritePath, 32f, FilterMode.Bilinear);

        ConfigureSingleSprite(SkyBgPath, 32f, FilterMode.Point);
        ConfigureSingleSprite(MountainsBgPath, 32f, FilterMode.Point);
        ConfigureSingleSprite(DungeonBgPath, 32f, FilterMode.Point);
        ConfigureSingleSprite(CaveBgPath, 32f, FilterMode.Point);

        for (int i = 0; i < 10; i++)
            ConfigureSingleSprite(ScorpionVioletPrefix + i + ".png", 100f, FilterMode.Point);
    }

    private static void ConfigureSingleSprite(string path, float ppu, FilterMode filterMode)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[Level2Builder] Brak tekstury: " + path);
            return;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, ppu))
        {
            importer.spritePixelsPerUnit = ppu;
            changed = true;
        }
        if (importer.filterMode != filterMode)
        {
            importer.filterMode = filterMode;
            changed = true;
        }
        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static Material ResolveSpriteMaterial()
    {
        GameObject existingCrystal = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
        if (existingCrystal != null)
        {
            SpriteRenderer sr = existingCrystal.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sharedMaterial != null && sr.sharedMaterial.name != "Default-Line")
                return sr.sharedMaterial;
        }

        Material cainosMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Cainos/Pixel Art Platformer - Village Props/Material/MT Village Props - Default.mat");
        if (cainosMat != null)
            return cainosMat;

        GameObject temp = new GameObject("TempMatCheck");
        Material fallback = temp.AddComponent<SpriteRenderer>().sharedMaterial;
        Object.DestroyImmediate(temp);
        return fallback;
    }

    private static void BuildVioletScorpionPrefab(Material spriteMat)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        var frames = new Sprite[10];
        for (int i = 0; i < 10; i++)
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(ScorpionVioletPrefix + i + ".png");

        GameObject sc = new GameObject("VioletScorpion");
        sc.transform.localScale = new Vector3(4.5f, 4.5f, 1f);

        SpriteRenderer sr = sc.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sharedMaterial = spriteMat;
        sr.sortingOrder = 6;

        Rigidbody2D rb = sc.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = sc.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.13f, 0.09f);
        col.offset = new Vector2(0f, -0.025f);

        PatrolEnemy patrol = sc.AddComponent<PatrolEnemy>();
        SerializedObject pso = new SerializedObject(patrol);
        pso.FindProperty("moveSpeed").floatValue = 2.15f;
        pso.FindProperty("patrolLeft").floatValue = 2.2f;
        pso.FindProperty("patrolRight").floatValue = 2.2f;
        pso.FindProperty("spriteFacesRight").boolValue = false;
        pso.FindProperty("spriteRenderer").objectReferenceValue = sr;
        pso.ApplyModifiedPropertiesWithoutUndo();

        StompableEnemy stomp = sc.AddComponent<StompableEnemy>();
        SerializedObject sso = new SerializedObject(stomp);
        sso.FindProperty("bounceVelocity").floatValue = 8.4f;
        sso.ApplyModifiedPropertiesWithoutUndo();

        SpriteFrameAnimator anim = sc.AddComponent<SpriteFrameAnimator>();
        SerializedObject aso = new SerializedObject(anim);
        SerializedProperty fp = aso.FindProperty("frames");
        fp.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            fp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        aso.FindProperty("framesPerSecond").floatValue = 11f;
        aso.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(sc, VioletScorpionPrefabPath);
        Object.DestroyImmediate(sc);
    }

    private static void BuildLevel2SceneContents(Material spriteMat)
    {
        // 1. GameManager (Restart + Transition)
        GameObject gmGo = new GameObject("GameManager");
        LevelRestart levelRestart = gmGo.AddComponent<LevelRestart>();
        LevelTransition levelTransition = gmGo.AddComponent<LevelTransition>();
        SerializedObject ltSo = new SerializedObject(levelTransition);
        ltSo.FindProperty("nextSceneName").stringValue = "Level3";
        ltSo.ApplyModifiedPropertiesWithoutUndo();

        // 2. Player
        GameObject playerGo = CreatePlayer(spriteMat, out BallController ball, out PlayerDeath playerDeath);

        // 3. Camera
        Camera mainCam = CreateCamera(playerGo);

        // 4. Parallax Background (nocna twierdza / jaskinie)
        CreateParallax(mainCam.transform, spriteMat);

        // 5. Tilemap (kamienne bloki)
        BuildLevel2Tilemap(spriteMat);

        // 6. Strefy rozgrywki (checkpointy, wrogowie, skrzynie, ruchome platformy, wyrzutnie, kolce, krysztaly, dekoracje)
        PopulateLevel2Zones(spriteMat);

        // 7. Oltarz wyjscia
        ExitGate exitGate = CreateExitGate(spriteMat, ball);

        // 8. UI Canvas + EventSystem
        CreateLevel2UI(ball, playerDeath, exitGate, levelRestart, levelTransition);
        CreateEventSystem();
    }

    private static GameObject CreatePlayer(Material spriteMat, out BallController ball, out PlayerDeath playerDeath)
    {
        GameObject playerGo = new GameObject("Player");
        playerGo.tag = "Player";
        playerGo.transform.position = new Vector3(-4.0f, 0.65f, 0f);
        playerGo.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        SpriteRenderer sr = playerGo.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerOrbSpritePath);
        sr.sharedMaterial = spriteMat;
        sr.color = Color.white;
        sr.sortingOrder = 8;

        CircleCollider2D cc = playerGo.AddComponent<CircleCollider2D>();
        cc.radius = 0.45f;

        Rigidbody2D rb = playerGo.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 0.25f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PlayerPhysMatPath);

        ball = playerGo.AddComponent<BallController>();
        ball.moveSpeed = 5.5f;
        ball.maxSpeed = 6.5f;
        ball.jumpForce = 7.2f;
        ball.groundLayer = ~0;
        ball.groundCheckDistance = 0.18f;

        playerGo.AddComponent<PlayerRespawn>();
        playerDeath = playerGo.AddComponent<PlayerDeath>();
        SerializedObject pdSo = new SerializedObject(playerDeath);
        pdSo.FindProperty("fallDeathY").floatValue = -9f;
        pdSo.FindProperty("respawnInvulnerabilityDuration").floatValue = 0.6f;
        pdSo.ApplyModifiedPropertiesWithoutUndo();

        return playerGo;
    }

    private static Camera CreateCamera(GameObject playerGo)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(-2.8f, 1.55f, -10f);

        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.14f, 0.24f, 1f);

        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();

        FollowCamera follow = camGo.AddComponent<FollowCamera>();
        follow.target = playerGo;
        SerializedObject fcSo = new SerializedObject(follow);
        fcSo.FindProperty("offset").vector2Value = new Vector2(1.2f, 0.9f);
        fcSo.FindProperty("smoothTime").floatValue = 0.14f;
        fcSo.FindProperty("minY").floatValue = -1.5f;
        fcSo.ApplyModifiedPropertiesWithoutUndo();

        return cam;
    }

    private static void CreateParallax(Transform camTransform, Material spriteMat)
    {
        GameObject root = new GameObject("ParallaxBackground");
        ParallaxBackground parallax = root.AddComponent<ParallaxBackground>();

        Sprite skySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkyBgPath);
        Sprite mntSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MountainsBgPath);
        Sprite dungeonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DungeonBgPath);
        Sprite caveSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaveBgPath);

        Transform skyLayer = CreateTiledLayer(root.transform, "Layer_Sky", skySprite, spriteMat, 3.5f, -30, 22f,
            new Color(0.28f, 0.34f, 0.56f, 1f));
        Transform mntLayer = CreateTiledLayer(root.transform, "Layer_Mountains", mntSprite, spriteMat, 1.5f, -25, 22f,
            new Color(0.45f, 0.52f, 0.72f, 1f));
        Transform dungeonLayer = CreateTiledLayer(root.transform, "Layer_Dungeon", dungeonSprite, spriteMat, 0.0f, -22, 22f,
            new Color(0.62f, 0.68f, 0.84f, 1f));
        Transform caveLayer = CreateTiledLayer(root.transform, "Layer_Cave", caveSprite, spriteMat, -1.2f, -18, 22f,
            new Color(0.55f, 0.60f, 0.74f, 1f));

        SerializedObject pso = new SerializedObject(parallax);
        pso.FindProperty("targetCamera").objectReferenceValue = camTransform;
        pso.FindProperty("verticalParallaxRatio").floatValue = 0.25f;

        SerializedProperty layersProp = pso.FindProperty("layers");
        layersProp.arraySize = 4;
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(0), skyLayer, 0.95f, 22f);
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(1), mntLayer, 0.75f, 22f);
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(2), dungeonLayer, 0.55f, 22f);
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(3), caveLayer, 0.35f, 22f);
        pso.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform CreateTiledLayer(
        Transform parent, string name, Sprite sprite, Material mat, float yPos, int sortingOrder, float tileWidth, Color tint)
    {
        GameObject layerGo = new GameObject(name);
        layerGo.transform.SetParent(parent, false);
        layerGo.transform.position = new Vector3(0f, yPos, 10f);

        for (int i = -2; i <= 8; i++)
        {
            GameObject seg = new GameObject("Seg_" + (i + 2));
            seg.transform.SetParent(layerGo.transform, false);
            seg.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);

            SpriteRenderer sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.color = tint;
            sr.sortingOrder = sortingOrder;
        }

        return layerGo.transform;
    }

    private static void SetParallaxEntry(SerializedProperty element, Transform t, float factor, float width)
    {
        element.FindPropertyRelative("layerTransform").objectReferenceValue = t;
        element.FindPropertyRelative("parallaxFactor").floatValue = factor;
        element.FindPropertyRelative("tileWidth").floatValue = width;
    }

    private static void BuildLevel2Tilemap(Material spriteMat)
    {
        GameObject gridGo = new GameObject("Grid", typeof(Grid));
        GameObject tmGo = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        tmGo.transform.SetParent(gridGo.transform, false);

        Tilemap tm = tmGo.GetComponent<Tilemap>();
        TilemapRenderer tr = tmGo.GetComponent<TilemapRenderer>();
        tr.sortingOrder = 5;
        tr.sharedMaterial = spriteMat;

        TileBase stoneTile = AssetDatabase.LoadAssetAtPath<TileBase>(StoneRuleTilePath);
        if (stoneTile == null)
            stoneTile = AssetDatabase.LoadAssetAtPath<TileBase>(StoneFallbackTilePath);

        if (stoneTile == null)
        {
            Debug.LogError("[Level2Builder] Nie znaleziono kafelka StoneRuleTile ani StoneTileMap_1!");
            return;
        }

        void FillSolid(int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    tm.SetTile(new Vector3Int(x, y, 0), stoneTile);
        }

        // Strefa 1: Brama Twierdzy
        FillSolid(-10, -9, -4, 6);    // sciana zamykajaca lewa krawedz
        FillSolid(-8, 10, -4, -1);    // grunt startowy (y=0)
        FillSolid(1, 3, 0, 0);        // niski stopien (y=1)
        // x=11..13 to przepasc 1
        FillSolid(14, 26, -4, -1);    // grunt przed klifem (y=0)

        // Strefa 2: Ruchome Kladki nad Przepascia
        FillSolid(27, 35, -4, 3);     // wysoki klif (y=4, wymaga BouncePad #1)
        // x=36..54 to wielka przepasc ze srodkowym filarem
        FillSolid(46, 48, -4, 3);     // srodkowy filar (y=4)
        FillSolid(55, 65, -4, 1);     // plaskowyz za winda (y=2)

        // Strefa 3: Krypta Kolcow i Zagadka ze Skrzynia
        FillSolid(66, 95, -4, -1);    // grunt glowny (y=0)
        FillSolid(71, 72, 0, 0);      // bezpieczna wysepka pomiedzy kolcami (y=1)
        FillSolid(87, 95, 0, 2);      // mur twierdzy (y=3, wymaga PushableCrate #1)

        // Strefa 4: Podniebne Filary i Katapulta (kanion x=96..125)
        FillSolid(98, 101, -1, 0);    // Wyspa A (y=1, z BouncePad #2)
        FillSolid(103, 108, 3, 4);    // Wysoka Wyspa B (y=5)
        FillSolid(119, 123, 1, 2);    // Wyspa C (y=3, z Checkpoint #5)

        // Strefa 5: Bastion Skarbow i Finalowy Oltarz
        FillSolid(126, 160, -4, -1);  // grunt glowny (y=0)
        FillSolid(126, 128, 0, 0);    // taras ladowania (y=1)
        FillSolid(135, 138, 0, 2);    // Wieza Skarbow (y=3, wymaga PushableCrate #2)
        FillSolid(139, 140, 0, 0);    // stopien zejscia (y=1)
        FillSolid(151, 152, 0, 0);    // pierwszy stopien przed oltarzem (y=1)
        FillSolid(153, 157, 0, 1);    // podwyzszenie oltarza (y=2)
        FillSolid(158, 160, 0, 6);    // sciana zamykajaca prawa krawedz

        tm.RefreshAllTiles();
        tm.CompressBounds();
        EditorUtility.SetDirty(tm);
    }

    private static void PopulateLevel2Zones(Material spriteMat)
    {
        GameObject cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
        GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CratePrefabPath);
        GameObject scorpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScorpionPrefabPath);
        GameObject skelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath);
        GameObject violetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VioletScorpionPrefabPath);
        Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowSpritePath);

        Transform gCheckpoints = new GameObject("Checkpoints").transform;
        Transform gEnemies = new GameObject("Enemies").transform;
        Transform gCrates = new GameObject("PushableCrates").transform;
        Transform gCrystals = new GameObject("Crystals").transform;
        Transform gMoving = new GameObject("MovingPlatforms").transform;
        Transform gBouncePads = new GameObject("BouncePads").transform;
        Transform gObstacles = new GameObject("LevelObstacles").transform;
        Transform gHazards = new GameObject("Hazards").transform;
        Transform gDecor = new GameObject("Decor_Level2").transform;

        GameObject Spawn(GameObject prefab, Transform parent, Vector3 pos, string customName = null)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = pos;
            if (!string.IsNullOrEmpty(customName))
                go.name = customName;
            return go;
        }

        GameObject SpawnProp(string propName, Transform parent, Vector3 pos, int sortOrder = -1, bool stripColliders = false)
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + propName + ".prefab");
            if (pf == null)
            {
                Debug.LogWarning("[Level2Builder] Brak propu: " + propName);
                return null;
            }
            GameObject go = Spawn(pf, parent, pos);
            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingOrder = sortOrder;
            foreach (ParticleSystemRenderer psr in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
                psr.sortingOrder = sortOrder + 1;
            if (stripColliders)
            {
                foreach (Collider2D c in go.GetComponentsInChildren<Collider2D>(true))
                    Object.DestroyImmediate(c);
            }
            return go;
        }

        // --- 1. CHECKPOINTY (6 sztandarow) ---
        var cpPositions = new Vector3[]
        {
            new Vector3(29.0f, 4.0f, 0f),
            new Vector3(57.0f, 2.0f, 0f),
            new Vector3(77.0f, 0.0f, 0f),
            new Vector3(94.0f, 3.0f, 0f),
            new Vector3(121.0f, 3.0f, 0f),
            new Vector3(149.2f, 0.0f, 0f),
        };
        for (int i = 0; i < cpPositions.Length; i++)
            Spawn(cpPrefab, gCheckpoints, cpPositions[i], "Checkpoint " + (i + 1));

        // --- 2. PRZECIWNICY (7 sztuk: Skorpiony, Szkielety, Fioletowe Skorpiony) ---
        void SpawnEnemy(GameObject prefab, Vector3 pos, float left, float right, float speed, string enemyName)
        {
            GameObject go = Spawn(prefab, gEnemies, pos, enemyName);
            PatrolEnemy pe = go.GetComponent<PatrolEnemy>();
            SerializedObject so = new SerializedObject(pe);
            so.FindProperty("patrolLeft").floatValue = left;
            so.FindProperty("patrolRight").floatValue = right;
            so.FindProperty("moveSpeed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        SpawnEnemy(scorpPrefab, new Vector3(19.5f, 0.36f, 0f), 2.5f, 2.5f, 1.6f, "Scorpion 1");
        SpawnEnemy(skelPrefab, new Vector3(32.8f, 4.45f, 0f), 1.2f, 1.2f, 1.4f, "Skeleton 1");
        SpawnEnemy(violetPrefab, new Vector3(61.5f, 2.36f, 0f), 2.0f, 2.0f, 2.1f, "VioletScorpion 1");
        SpawnEnemy(skelPrefab, new Vector3(90.0f, 3.45f, 0f), 1.5f, 1.5f, 1.45f, "Skeleton 2");
        SpawnEnemy(violetPrefab, new Vector3(106.0f, 5.36f, 0f), 1.5f, 1.5f, 2.2f, "VioletScorpion 2");
        SpawnEnemy(violetPrefab, new Vector3(143.5f, 0.36f, 0f), 1.8f, 1.8f, 2.3f, "VioletScorpion 3");
        SpawnEnemy(skelPrefab, new Vector3(147.0f, 0.45f, 0f), 1.2f, 1.2f, 1.6f, "Skeleton 3");

        // --- 3. PRZESUWALNE SKRZYNIE ---
        Spawn(cratePrefab, gCrates, new Vector3(82.0f, 0.72f, 0f), "PushableCrate 1 (mur)");
        Spawn(cratePrefab, gCrates, new Vector3(131.5f, 0.72f, 0f), "PushableCrate 2 (wieza)");

        // --- 4. RUCHOME PLATFORMY (nowa mechanika) ---
        void SpawnMovingPlatform(string platName, Vector3 pos, Vector2 moveOffset, float speed, float pause)
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Platform 02 X3.prefab");
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pf, gMoving);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = platName;
            go.transform.position = pos;

            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingOrder = 4;

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            MovingPlatform2D mp = go.AddComponent<MovingPlatform2D>();
            SerializedObject mpSo = new SerializedObject(mp);
            mpSo.FindProperty("moveOffset").vector2Value = moveOffset;
            mpSo.FindProperty("speed").floatValue = speed;
            mpSo.FindProperty("pauseDuration").floatValue = pause;
            mpSo.ApplyModifiedPropertiesWithoutUndo();
        }

        SpawnMovingPlatform("MovingPlatform 1 (pozioma)", new Vector3(36.5f, 4.0f, 0f), new Vector2(6.0f, 0f), 2.4f, 0.5f);
        SpawnMovingPlatform("MovingPlatform 2 (winda pionowa)", new Vector3(50.0f, 1.5f, 0f), new Vector2(0f, 3.0f), 2.2f, 0.55f);
        SpawnMovingPlatform("MovingPlatform 3 (wahadlo kanionu)", new Vector3(109.5f, 4.5f, 0f), new Vector2(6.0f, 0f), 2.6f, 0.45f);

        // --- 5. SPREZYSTE WYRZUTNIE (nowa mechanika) ---
        void SpawnBouncePad(string padName, Vector3 pos)
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Bounding Platform 01.prefab");
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pf, gBouncePads);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = padName;
            go.transform.position = pos;

            var oldBp = go.GetComponent<Cainos.PixelArtPlatformer_VillageProps.BoundingPlatform>();
            if (oldBp != null) Object.DestroyImmediate(oldBp);

            foreach (Collider2D rootCol in go.GetComponents<Collider2D>())
                Object.DestroyImmediate(rootCol);

            Transform baseTr = go.transform.Find("Base");
            if (baseTr != null)
            {
                SpriteRenderer baseSr = baseTr.GetComponent<SpriteRenderer>();
                if (baseSr != null) baseSr.sortingOrder = 3;
                foreach (Collider2D bc in baseTr.GetComponents<Collider2D>())
                    Object.DestroyImmediate(bc);
            }

            Transform platTr = go.transform.Find("Platform");
            if (platTr != null)
            {
                platTr.localPosition = new Vector3(0f, -0.82f, 0.01f);

                Rigidbody2D platRb = platTr.GetComponent<Rigidbody2D>();
                if (platRb != null) Object.DestroyImmediate(platRb);

                foreach (Collider2D pc in platTr.GetComponents<Collider2D>())
                    Object.DestroyImmediate(pc);

                SpriteRenderer platSr = platTr.GetComponent<SpriteRenderer>();
                if (platSr != null)
                {
                    platSr.sortingOrder = 4;
                    platSr.color = new Color(0.65f, 1f, 0.95f, 1f);
                }

                BoxCollider2D topCol = platTr.gameObject.AddComponent<BoxCollider2D>();
                topCol.offset = new Vector2(0f, 1.08f);
                topCol.size = new Vector2(1.94f, 0.18f);

                GameObject glowGo = new GameObject("Glow");
                glowGo.transform.SetParent(platTr, false);
                glowGo.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                glowGo.transform.localScale = new Vector3(2.2f, 0.7f, 1f);
                SpriteRenderer glowSr = glowGo.AddComponent<SpriteRenderer>();
                glowSr.sprite = glowSprite;
                glowSr.sharedMaterial = spriteMat;
                glowSr.color = new Color(0.15f, 0.95f, 0.9f, 0.45f);
                glowSr.sortingOrder = 3;

                BouncePad bp = platTr.gameObject.AddComponent<BouncePad>();
                SerializedObject bpSo = new SerializedObject(bp);
                bpSo.FindProperty("bounceForce").floatValue = 11.2f;
                bpSo.FindProperty("cooldown").floatValue = 0.15f;
                bpSo.FindProperty("visualTransform").objectReferenceValue = platTr;
                bpSo.FindProperty("popHeight").floatValue = 0.30f;
                bpSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        SpawnBouncePad("BouncePad 1 (klif)", new Vector3(25.0f, 0.0f, 0f));
        SpawnBouncePad("BouncePad 2 (kanion)", new Vector3(100.0f, 1.0f, 0f));

        // --- 6. STATYCZNE PLATFORMY I KOLCE ---
        SpawnProp("Platform 02 X3", gObstacles, new Vector3(70.2f, 2.55f, 0f), 3);
        SpawnProp("Platform 02 X3", gObstacles, new Vector3(143.2f, 2.0f, 0f), 3);

        float[] spikeXs = { 69.2f, 70.35f, 73.6f, 74.75f };
        for (int i = 0; i < spikeXs.Length; i++)
        {
            GameObject spike = SpawnProp("Spike", gHazards, new Vector3(spikeXs[i], 0.12f, 0f), 4);
            if (spike != null)
            {
                spike.name = "Spike " + (i + 1);
                BoxCollider2D scol = spike.GetComponent<BoxCollider2D>();
                if (scol == null) scol = spike.AddComponent<BoxCollider2D>();
                scol.isTrigger = true;
                scol.size = new Vector2(0.9f, 0.28f);
                scol.offset = new Vector2(0f, 0.08f);
                if (spike.GetComponent<HazardSpike>() == null)
                    spike.AddComponent<HazardSpike>();
            }
        }

        // --- 7. KRYSZTALY (24 sztuki, wymagane 18) ---
        var crystalPositions = new Vector3[]
        {
            // Strefa 1 (5)
            new Vector3(2.5f, 1.85f, 0f),
            new Vector3(12.5f, 2.0f, 0f),
            new Vector3(19.5f, 1.35f, 0f),
            new Vector3(25.5f, 3.8f, 0f),
            new Vector3(26.8f, 5.1f, 0f),
            // Strefa 2 (5)
            new Vector3(41.0f, 5.2f, 0f),
            new Vector3(47.5f, 4.9f, 0f),
            new Vector3(51.6f, 5.6f, 0f),
            new Vector3(51.6f, 2.5f, 0f),
            new Vector3(61.5f, 3.3f, 0f),
            // Strefa 3 (4)
            new Vector3(71.7f, 3.55f, 0f),
            new Vector3(72.0f, 1.9f, 0f),
            new Vector3(89.0f, 3.9f, 0f),
            new Vector3(91.8f, 3.9f, 0f),
            // Strefa 4 (4)
            new Vector3(101.8f, 5.2f, 0f),
            new Vector3(106.0f, 6.3f, 0f),
            new Vector3(114.0f, 5.7f, 0f),
            new Vector3(122.2f, 3.9f, 0f),
            // Strefa 5 (6)
            new Vector3(135.8f, 3.9f, 0f),
            new Vector3(137.0f, 4.4f, 0f),
            new Vector3(138.2f, 3.9f, 0f),
            new Vector3(144.7f, 3.0f, 0f),
            new Vector3(146.5f, 1.0f, 0f),
            new Vector3(151.8f, 2.0f, 0f),
        };
        for (int i = 0; i < crystalPositions.Length; i++)
            Spawn(crystalPrefab, gCrystals, crystalPositions[i], "Crystal " + (i + 1));

        // --- 8. DEKORACJE W TLE (wszystkie bez colliderow) ---
        // Strefa 1
        SpawnProp("Road Sign 02", gDecor, new Vector3(-2.0f, 0f, 0f), -1, true);
        SpawnProp("Torch", gDecor, new Vector3(-6.2f, 1.6f, 0f), -1, true);
        SpawnProp("Brick Wall 01", gDecor, new Vector3(-0.5f, 0f, 0f), -3, true);
        SpawnProp("Campfire 01", gDecor, new Vector3(7.5f, 0f, 0f), -1, true);
        SpawnProp("Gravestone 01", gDecor, new Vector3(15.5f, 0f, 0f), -2, true);
        SpawnProp("Torch", gDecor, new Vector3(22.8f, 1.4f, 0f), -1, true);
        // Strefa 2
        SpawnProp("Brick Wall 01", gDecor, new Vector3(31.2f, 4.0f, 0f), -3, true);
        SpawnProp("Road Lamp", gDecor, new Vector3(34.5f, 4.0f, 0f), -1, true);
        SpawnProp("Rock 02", gDecor, new Vector3(47.2f, 4.0f, 0f), -2, true);
        SpawnProp("Chest Iron", gDecor, new Vector3(58.5f, 2.0f, 0f), -1, true);
        SpawnProp("Barrel", gDecor, new Vector3(64.2f, 2.0f, 0f), -1, true);
        // Strefa 3
        SpawnProp("Gravestone 02", gDecor, new Vector3(67.3f, 0f, 0f), -2, true);
        SpawnProp("Gravestone 03", gDecor, new Vector3(79.5f, 0f, 0f), -2, true);
        SpawnProp("Torch", gDecor, new Vector3(84.5f, 1.5f, 0f), -1, true);
        SpawnProp("Brick Wall 01", gDecor, new Vector3(92.0f, 3.0f, 0f), -3, true);
        // Strefa 4
        SpawnProp("Rock 01", gDecor, new Vector3(99.0f, 1.0f, 0f), -2, true);
        SpawnProp("Gravestone 04", gDecor, new Vector3(104.0f, 5.0f, 0f), -2, true);
        SpawnProp("Road Lamp", gDecor, new Vector3(120.2f, 3.0f, 0f), -1, true);
        // Strefa 5
        SpawnProp("Rock 03", gDecor, new Vector3(127.5f, 1.0f, 0f), -2, true);
        SpawnProp("Banner", gDecor, new Vector3(130.0f, 0f, 0f), -2, true);
        SpawnProp("Chest Silver", gDecor, new Vector3(136.5f, 3.0f, 0f), -1, true);
        SpawnProp("Chest Golden", gDecor, new Vector3(137.8f, 3.0f, 0f), -1, true);
        SpawnProp("Statue", gDecor, new Vector3(141.2f, 0f, 0f), -2, true);
        SpawnProp("Fire Bowl", gDecor, new Vector3(153.8f, 2.0f, 0f), 4, true);
        SpawnProp("Fire Bowl", gDecor, new Vector3(156.6f, 2.0f, 0f), 4, true);
    }

    private static ExitGate CreateExitGate(Material spriteMat, BallController player)
    {
        GameObject stoneSrc = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Stone of Recall.prefab");
        Sprite baseSprite = stoneSrc.GetComponent<SpriteRenderer>().sprite;
        Sprite glowSprite = null;
        Transform srcGlow = stoneSrc.transform.Find("Glow");
        if (srcGlow != null)
            glowSprite = srcGlow.GetComponent<SpriteRenderer>().sprite;

        GameObject gateGo = new GameObject("Exitgate");
        gateGo.transform.position = new Vector3(155.2f, 2.0f, 0f);
        gateGo.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

        SpriteRenderer sr = gateGo.AddComponent<SpriteRenderer>();
        sr.sprite = baseSprite;
        sr.sharedMaterial = spriteMat;
        sr.color = Color.white;
        sr.sortingOrder = 5;

        BoxCollider2D col = gateGo.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.75f, 0.9f);
        col.offset = new Vector2(0f, 0.45f);

        GameObject glowGo = new GameObject("RuneGlow");
        glowGo.transform.SetParent(gateGo.transform, false);
        glowGo.transform.localPosition = srcGlow != null ? srcGlow.localPosition : new Vector3(0.06f, 0.59f, -0.01f);
        SpriteRenderer glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = glowSprite != null ? glowSprite : AssetDatabase.LoadAssetAtPath<Sprite>(GlowSpritePath);
        glowSr.sharedMaterial = spriteMat;
        glowSr.sortingOrder = 6;

        AudioSource audio = gateGo.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioClipPath);

        ExitGate gate = gateGo.AddComponent<ExitGate>();
        SerializedObject gso = new SerializedObject(gate);
        gso.FindProperty("player").objectReferenceValue = player;
        gso.FindProperty("requiredPoints").intValue = RequiredPoints;
        gso.FindProperty("glowRenderer").objectReferenceValue = glowSr;
        gso.FindProperty("unlockAudio").objectReferenceValue = audio;
        gso.ApplyModifiedPropertiesWithoutUndo();

        return gate;
    }

    private static void CreateLevel2UI(
        BallController player,
        PlayerDeath playerDeath,
        ExitGate exitGate,
        LevelRestart levelRestart,
        LevelTransition levelTransition)
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Sprite crystalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalSpritePath);
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
        if (mainFont != null && mainFont.fallbackFontAssetTable != null)
        {
            foreach (TMP_FontAsset fb in mainFont.fallbackFontAssetTable)
            {
                if (fb != null && fb.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                    fb.TryAddCharacters("ąęćłńóśźżĄĘĆŁŃÓŚŹŻ");
            }
        }

        // --- 1. HUD PANEL ---
        GameObject hudPanel = MakePanel("HUDPanel", canvas.transform, new Color(0.06f, 0.09f, 0.16f, 0.85f), uiSprite);
        RectTransform hudRt = hudPanel.GetComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(0f, 1f);
        hudRt.pivot = new Vector2(0f, 1f);
        hudRt.anchoredPosition = new Vector2(28f, -24f);
        hudRt.sizeDelta = new Vector2(580f, 146f);

        TMP_Text levelBadge = MakeText("LevelBadge", hudPanel.transform, "POZIOM 2 • KAMIENNA TWIERDZA", 19f,
            new Color(0.75f, 0.86f, 1.0f, 0.9f), TextAlignmentOptions.Left, FontStyles.Bold);
        RectTransform badgeRt = levelBadge.rectTransform;
        badgeRt.anchorMin = new Vector2(0f, 0.72f);
        badgeRt.anchorMax = new Vector2(1f, 1f);
        badgeRt.offsetMin = new Vector2(98f, 0f);
        badgeRt.offsetMax = new Vector2(-18f, -10f);

        GameObject iconGo = new GameObject("CrystalIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconGo.transform.SetParent(hudPanel.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(20f, 0f);
        iconRt.sizeDelta = new Vector2(62f, 62f);
        Image iconImg = iconGo.GetComponent<Image>();
        iconImg.sprite = crystalSprite;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        TMP_Text scoreText = MakeText("Scoretext", hudPanel.transform, "Kryształy: 0 / " + RequiredPoints, 34f,
            new Color(0.35f, 0.95f, 1.0f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);
        RectTransform scoreRt = scoreText.rectTransform;
        scoreRt.anchorMin = new Vector2(0f, 0.36f);
        scoreRt.anchorMax = new Vector2(1f, 0.74f);
        scoreRt.offsetMin = new Vector2(98f, 0f);
        scoreRt.offsetMax = new Vector2(-18f, 0f);

        TMP_Text goalText = MakeText("Goaltext", hudPanel.transform, "Cel: Zbierz " + RequiredPoints + " kryształów (0/" + RequiredPoints + ")", 21f,
            new Color(0.92f, 0.95f, 1.0f, 0.95f), TextAlignmentOptions.Left, FontStyles.Normal);
        RectTransform goalRt = goalText.rectTransform;
        goalRt.anchorMin = new Vector2(0f, 0f);
        goalRt.anchorMax = new Vector2(1f, 0.38f);
        goalRt.offsetMin = new Vector2(98f, 12f);
        goalRt.offsetMax = new Vector2(-18f, 0f);

        ScoreDisplay scoreDisplay = canvasGo.AddComponent<ScoreDisplay>();
        SerializedObject sdSo = new SerializedObject(scoreDisplay);
        sdSo.FindProperty("player").objectReferenceValue = player;
        sdSo.FindProperty("exitGate").objectReferenceValue = exitGate;
        sdSo.FindProperty("scoreText").objectReferenceValue = scoreText;
        sdSo.ApplyModifiedPropertiesWithoutUndo();

        // --- 2. WIN PANEL ---
        GameObject winPanel = MakeFullscreenOverlay("WinPanel", canvas.transform, new Color(0.03f, 0.06f, 0.10f, 0.88f));
        GameObject winCard = MakeCenteredCard("Card", winPanel.transform, new Vector2(800f, 500f),
            new Color(0.10f, 0.16f, 0.25f, 0.98f), uiSprite);

        TMP_Text winTitle = MakeText("Wintitle", winCard.transform, "POZIOM 2 UKOŃCZONY!", 58f,
            new Color(0.32f, 0.95f, 0.56f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(winTitle.rectTransform, new Vector2(0f, 145f), new Vector2(740f, 100f));

        TMP_Text winSub = MakeText("WinSubtitle", winCard.transform, "Kamienna Twierdza zdobyta! Ołtarz runiczny aktywowany!", 27f,
            new Color(0.92f, 0.96f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetCenteredRect(winSub.rectTransform, new Vector2(0f, 60f), new Vector2(740f, 60f));

        Button nextLevelBtn = MakeButton("NextLevelButton", winCard.transform, "Następny poziom",
            new Vector2(0f, -45f), new Vector2(420f, 80f), new Color(0.18f, 0.72f, 0.42f, 1f), uiSprite);

        Button winRestartBtn = MakeButton("Restartbutton", winCard.transform, "Zagraj ponownie",
            new Vector2(0f, -145f), new Vector2(420f, 80f), new Color(0.24f, 0.34f, 0.52f, 1f), uiSprite);

        UnityEventTools.AddVoidPersistentListener(nextLevelBtn.onClick, new UnityAction(levelTransition.LoadNextLevel));
        UnityEventTools.AddVoidPersistentListener(winRestartBtn.onClick, new UnityAction(levelRestart.RestartLevel));

        winPanel.SetActive(false);

        SerializedObject egSo = new SerializedObject(exitGate);
        egSo.FindProperty("goalText").objectReferenceValue = goalText;
        egSo.FindProperty("winPanel").objectReferenceValue = winPanel;
        egSo.FindProperty("hudRoot").objectReferenceValue = hudPanel;
        egSo.ApplyModifiedPropertiesWithoutUndo();

        // --- 3. DEATH MENU ---
        GameObject deathMenuGo = new GameObject("DeathMenu", typeof(RectTransform));
        deathMenuGo.transform.SetParent(canvas.transform, false);
        RectTransform dmRt = deathMenuGo.GetComponent<RectTransform>();
        dmRt.anchorMin = Vector2.zero;
        dmRt.anchorMax = Vector2.one;
        dmRt.offsetMin = Vector2.zero;
        dmRt.offsetMax = Vector2.zero;
        DeathMenu deathMenu = deathMenuGo.AddComponent<DeathMenu>();

        GameObject deathPanel = MakeFullscreenOverlay("Panel", deathMenuGo.transform, new Color(0.05f, 0.04f, 0.08f, 0.90f));
        GameObject deathCard = MakeCenteredCard("Card", deathPanel.transform, new Vector2(780f, 540f),
            new Color(0.13f, 0.12f, 0.20f, 0.98f), uiSprite);

        TMP_Text dTitle = MakeText("Title", deathCard.transform, "PORAŻKA!", 78f,
            new Color(0.95f, 0.25f, 0.28f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(dTitle.rectTransform, new Vector2(0f, 175f), new Vector2(700f, 100f));

        TMP_Text dReason = MakeText("Reason", deathCard.transform, "Spadłeś w przepaść!", 34f,
            Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
        SetCenteredRect(dReason.rectTransform, new Vector2(0f, 88f), new Vector2(700f, 60f));

        TMP_Text dScore = MakeText("Score", deathCard.transform, "Zdobyte kryształy: 0", 40f,
            new Color(1.0f, 0.85f, 0.25f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(dScore.rectTransform, new Vector2(0f, 14f), new Vector2(700f, 70f));

        Button respawnBtn = MakeButton("RespawnButton", deathCard.transform, "Wróć do checkpointu",
            new Vector2(0f, -85f), new Vector2(450f, 80f), new Color(0.18f, 0.70f, 0.42f, 1f), uiSprite);

        Button restartBtn = MakeButton("RestartButton", deathCard.transform, "Zagraj od nowa",
            new Vector2(0f, -185f), new Vector2(450f, 80f), new Color(0.24f, 0.34f, 0.52f, 1f), uiSprite);

        UnityEventTools.AddVoidPersistentListener(respawnBtn.onClick, new UnityAction(deathMenu.RespawnAtCheckpoint));
        UnityEventTools.AddVoidPersistentListener(restartBtn.onClick, new UnityAction(deathMenu.RestartLevel));

        SerializedObject dmSo = new SerializedObject(deathMenu);
        dmSo.FindProperty("panel").objectReferenceValue = deathPanel;
        dmSo.FindProperty("hudRoot").objectReferenceValue = hudPanel;
        dmSo.FindProperty("reasonText").objectReferenceValue = dReason;
        dmSo.FindProperty("scoreText").objectReferenceValue = dScore;
        dmSo.FindProperty("player").objectReferenceValue = playerDeath;
        dmSo.FindProperty("levelRestart").objectReferenceValue = levelRestart;
        dmSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject pdSo = new SerializedObject(playerDeath);
        pdSo.FindProperty("deathMenu").objectReferenceValue = deathMenu;
        pdSo.ApplyModifiedPropertiesWithoutUndo();

        deathPanel.SetActive(false);

        LevelMenuBuilder.AttachInGameLevelMenuToCurrentScene("Level2");
    }

    private static void CreateEventSystem()
    {
        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private static void UpdateBuildSettings()
    {
        var ordered = new List<EditorBuildSettingsScene>();
        if (File.Exists(MainMenuScenePath))
            ordered.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));

        ordered.Add(new EditorBuildSettingsScene(SampleScenePath, true));
        ordered.Add(new EditorBuildSettingsScene(Level2ScenePath, true));

        if (File.Exists(Level3ScenePath))
            ordered.Add(new EditorBuildSettingsScene(Level3ScenePath, true));

        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing == null || string.IsNullOrEmpty(existing.path)) continue;
            if (existing.path == MainMenuScenePath || existing.path == SampleScenePath || existing.path == Level2ScenePath || existing.path == Level3ScenePath) continue;
            ordered.Add(existing);
        }

        EditorBuildSettings.scenes = ordered.ToArray();
    }

    private static void ConnectSampleSceneToLevel2()
    {
        Scene sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

        LevelRestart levelRestart = Object.FindFirstObjectByType<LevelRestart>();
        if (levelRestart == null)
        {
            GameObject gmGo = new GameObject("GameManager");
            levelRestart = gmGo.AddComponent<LevelRestart>();
        }

        LevelTransition levelTransition = levelRestart.GetComponent<LevelTransition>();
        if (levelTransition == null)
            levelTransition = levelRestart.gameObject.AddComponent<LevelTransition>();

        SerializedObject ltSo = new SerializedObject(levelTransition);
        ltSo.FindProperty("nextSceneName").stringValue = "Level2";
        ltSo.ApplyModifiedPropertiesWithoutUndo();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform cardTr = canvas.transform.Find("WinPanel/Card");
            if (cardTr != null)
            {
                RectTransform cardRt = cardTr.GetComponent<RectTransform>();
                if (cardRt != null)
                    cardRt.sizeDelta = new Vector2(780f, 500f);

                Transform titleTr = cardTr.Find("Wintitle");
                if (titleTr != null)
                {
                    RectTransform titleRt = titleTr.GetComponent<RectTransform>();
                    if (titleRt != null)
                        titleRt.anchoredPosition = new Vector2(0f, 145f);
                }

                Transform subTr = cardTr.Find("WinSubtitle");
                if (subTr != null)
                {
                    RectTransform subRt = subTr.GetComponent<RectTransform>();
                    if (subRt != null)
                        subRt.anchoredPosition = new Vector2(0f, 60f);
                }

                Transform oldNextBtn = cardTr.Find("NextLevelButton");
                if (oldNextBtn != null)
                    Object.DestroyImmediate(oldNextBtn.gameObject);

                Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                Button nextBtn = MakeButton("NextLevelButton", cardTr, "Następny poziom",
                    new Vector2(0f, -45f), new Vector2(420f, 80f), new Color(0.18f, 0.72f, 0.42f, 1f), uiSprite);
                UnityEventTools.AddVoidPersistentListener(nextBtn.onClick, new UnityAction(levelTransition.LoadNextLevel));

                Transform restartTr = cardTr.Find("Restartbutton");
                if (restartTr != null)
                {
                    RectTransform restartRt = restartTr.GetComponent<RectTransform>();
                    if (restartRt != null)
                    {
                        restartRt.anchoredPosition = new Vector2(0f, -145f);
                        restartRt.sizeDelta = new Vector2(420f, 80f);
                    }

                    Image restartImg = restartTr.GetComponent<Image>();
                    if (restartImg != null)
                        restartImg.color = new Color(0.24f, 0.34f, 0.52f, 1f);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(sampleScene);
        EditorSceneManager.SaveScene(sampleScene);
    }

    private static GameObject MakePanel(string name, Transform parent, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        return go;
    }

    private static GameObject MakeFullscreenOverlay(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static GameObject MakeCenteredCard(string name, Transform parent, Vector2 size, Color color, Sprite sprite)
    {
        GameObject card = MakePanel(name, parent, color, sprite);
        SetCenteredRect(card.GetComponent<RectTransform>(), Vector2.zero, size);
        return card;
    }

    private static TMP_Text MakeText(
        string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions align, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
        if (fontAsset != null) tmp.font = fontAsset;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button MakeButton(
        string name, Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor, Sprite sprite)
    {
        GameObject go = MakePanel(name, parent, bgColor, sprite);
        SetCenteredRect(go.GetComponent<RectTransform>(), pos, size);
        Button btn = go.AddComponent<Button>();

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = colors;

        TMP_Text txt = MakeText("Label", go.transform, label, 30f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }

    private static void SetCenteredRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
