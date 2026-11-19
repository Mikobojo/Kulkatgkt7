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
/// Narzedzie edytora generujace kompletny trzeci poziom gry (Assets/Scenes/Level3.unity - Ognista Cytadela),
/// nowy prefab zlotego skorpiona, dodajace wszystkie trzy sceny do Build Settings
/// oraz podpinajace przycisk "Nastepny poziom" w Level2.unity.
/// </summary>
public static class Level3Builder
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
    private const string GoldScorpionPrefabPath = "Assets/Prefabs/Enemies/GoldScorpion.prefab";

    private const string FireRuleTilePath = "Assets/SimpleRuleTiles/Fire/FireRuleTile.asset";
    private const string FireFallbackTilePath = "Assets/SimpleRuleTiles/Fire/FireTileMap_1.asset";
    private const string PlayerPhysMatPath = "Assets/karsiori/TileMap/Material/physicmaterial.physicsMaterial2D";

    private const string CainosPropsPrefix = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - ";
    private const string CrystalSpritePath = "Assets/Sprites/CrystalGem.png";
    private const string PlayerOrbSpritePath = "Assets/Sprites/PlayerOrb.png";
    private const string GlowSpritePath = "Assets/Sprites/SoftGlow.png";
    private const string AudioClipPath = "Assets/Audio/853656__coghezzi__sacred-rune-lock-holy-magic-seal-activation.wav";

    private const string SkyBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_01_sky.png";
    private const string MountainsBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_02_mountains.png";
    private const string DungeonBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_04_dungeon_g.png";
    private const string CaveInsideBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_06_caveinside_b.png";
    private const string ScorpionGoldPrefix = "Assets/Cave Platformer Tileset/Individual PNG files/Monster/scorpion-gold/scorpion-gold-";
    private const string MainFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private const int RequiredPoints = 20;

    private static bool pendingBuildAfterPlayMode;

    [InitializeOnLoadMethod]
    private static void AutoBuildIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(Level3ScenePath))
                return;

            BuildAll();
        };
    }

    [MenuItem("Tools/Build Level 3 (Ognista Cytadela)")]
    public static void BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!pendingBuildAfterPlayMode)
            {
                pendingBuildAfterPlayMode = true;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }
            EditorApplication.isPlaying = false;
            return;
        }

        BuildAllInternal();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        pendingBuildAfterPlayMode = false;
        EditorApplication.delayCall += BuildAllInternal;
    }

    private static void BuildAllInternal()
    {
        EditorSceneManager.SaveOpenScenes();

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        ConfigureTextures();
        Material spriteMat = ResolveSpriteMaterial();

        BuildGoldScorpionPrefab(spriteMat);

        Scene level3Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildLevel3SceneContents(spriteMat);
        EditorSceneManager.MarkSceneDirty(level3Scene);
        EditorSceneManager.SaveScene(level3Scene, Level3ScenePath);

        UpdateBuildSettings();
        ConnectLevel2ToLevel3();

        EditorSceneManager.OpenScene(Level3ScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();

        Debug.Log("[Level3Builder] Level 3 (Assets/Scenes/Level3.unity) oraz przejscie z Level2.unity wygenerowane pomyslnie!");
    }

    private static void ConfigureTextures()
    {
        ConfigureSingleSprite(CrystalSpritePath, 32f, FilterMode.Point);
        ConfigureSingleSprite(PlayerOrbSpritePath, 32f, FilterMode.Point);
        ConfigureSingleSprite(GlowSpritePath, 32f, FilterMode.Bilinear);

        ConfigureSingleSprite(SkyBgPath, 32f, FilterMode.Point);
        ConfigureSingleSprite(MountainsBgPath, 32f, FilterMode.Point);
        ConfigureSingleSprite(DungeonBgPath, 32f, FilterMode.Point);
        ConfigureSingleSprite(CaveInsideBgPath, 32f, FilterMode.Point);

        for (int i = 0; i < 10; i++)
            ConfigureSingleSprite(ScorpionGoldPrefix + i + ".png", 100f, FilterMode.Point);
    }

    private static void ConfigureSingleSprite(string path, float ppu, FilterMode filterMode)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[Level3Builder] Brak tekstury: " + path);
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

    private static void BuildGoldScorpionPrefab(Material spriteMat)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        var frames = new Sprite[10];
        for (int i = 0; i < 10; i++)
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(ScorpionGoldPrefix + i + ".png");

        GameObject sc = new GameObject("GoldScorpion");
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
        pso.FindProperty("moveSpeed").floatValue = 2.45f;
        pso.FindProperty("patrolLeft").floatValue = 2.2f;
        pso.FindProperty("patrolRight").floatValue = 2.2f;
        pso.FindProperty("spriteFacesRight").boolValue = false;
        pso.FindProperty("spriteRenderer").objectReferenceValue = sr;
        pso.ApplyModifiedPropertiesWithoutUndo();

        StompableEnemy stomp = sc.AddComponent<StompableEnemy>();
        SerializedObject sso = new SerializedObject(stomp);
        sso.FindProperty("bounceVelocity").floatValue = 8.6f;
        sso.ApplyModifiedPropertiesWithoutUndo();

        SpriteFrameAnimator anim = sc.AddComponent<SpriteFrameAnimator>();
        SerializedObject aso = new SerializedObject(anim);
        SerializedProperty fp = aso.FindProperty("frames");
        fp.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            fp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        aso.FindProperty("framesPerSecond").floatValue = 12f;
        aso.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(sc, GoldScorpionPrefabPath);
        Object.DestroyImmediate(sc);
    }

    private static void BuildLevel3SceneContents(Material spriteMat)
    {
        // 1. GameManager (Restart + Transition)
        GameObject gmGo = new GameObject("GameManager");
        LevelRestart levelRestart = gmGo.AddComponent<LevelRestart>();
        LevelTransition levelTransition = gmGo.AddComponent<LevelTransition>();
        SerializedObject ltSo = new SerializedObject(levelTransition);
        ltSo.FindProperty("nextSceneName").stringValue = "";
        ltSo.ApplyModifiedPropertiesWithoutUndo();

        // 2. Player
        GameObject playerGo = CreatePlayer(spriteMat, out BallController ball, out PlayerDeath playerDeath);

        // 3. Camera
        Camera mainCam = CreateCamera(playerGo);

        // 4. Parallax Background (wulkaniczno-ognista cytadela)
        CreateParallax(mainCam.transform, spriteMat);

        // 5. Tilemap (ogniste kafelki FireRuleTile)
        BuildLevel3Tilemap(spriteMat);

        // 6. Strefy rozgrywki (checkpointy, wrogowie, skrzynie, ruchome platformy, kruszace sie platformy,
        //    wirujace kule z kolcami, wyrzutnie, kolce, krysztaly, dekoracje)
        PopulateLevel3Zones(spriteMat);

        // 7. Oltarz wyjscia
        ExitGate exitGate = CreateExitGate(spriteMat, ball);

        // 8. UI Canvas + EventSystem
        CreateLevel3UI(ball, playerDeath, exitGate, levelRestart, levelTransition);
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
        cam.backgroundColor = new Color(0.15f, 0.06f, 0.08f, 1f);

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
        Sprite caveInsideSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaveInsideBgPath);

        Transform skyLayer = CreateTiledLayer(root.transform, "Layer_Sky", skySprite, spriteMat, 3.5f, -30, 22f,
            new Color(0.45f, 0.20f, 0.22f, 1f));
        Transform mntLayer = CreateTiledLayer(root.transform, "Layer_Mountains", mntSprite, spriteMat, 1.5f, -25, 22f,
            new Color(0.72f, 0.36f, 0.28f, 1f));
        Transform dungeonLayer = CreateTiledLayer(root.transform, "Layer_Dungeon", dungeonSprite, spriteMat, 0.0f, -22, 22f,
            new Color(0.82f, 0.48f, 0.38f, 1f));
        Transform caveInsideLayer = CreateTiledLayer(root.transform, "Layer_CaveInside", caveInsideSprite, spriteMat, -1.2f, -18, 22f,
            new Color(0.76f, 0.38f, 0.32f, 1f));

        SerializedObject pso = new SerializedObject(parallax);
        pso.FindProperty("targetCamera").objectReferenceValue = camTransform;
        pso.FindProperty("verticalParallaxRatio").floatValue = 0.25f;

        SerializedProperty layersProp = pso.FindProperty("layers");
        layersProp.arraySize = 4;
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(0), skyLayer, 0.95f, 22f);
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(1), mntLayer, 0.75f, 22f);
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(2), dungeonLayer, 0.55f, 22f);
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(3), caveInsideLayer, 0.35f, 22f);
        pso.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform CreateTiledLayer(
        Transform parent, string name, Sprite sprite, Material mat, float yPos, int sortingOrder, float tileWidth, Color tint)
    {
        GameObject layerGo = new GameObject(name);
        layerGo.transform.SetParent(parent, false);
        layerGo.transform.position = new Vector3(0f, yPos, 10f);

        for (int i = -2; i <= 9; i++)
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

    private static void BuildLevel3Tilemap(Material spriteMat)
    {
        GameObject gridGo = new GameObject("Grid", typeof(Grid));
        GameObject tmGo = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
        tmGo.transform.SetParent(gridGo.transform, false);

        Tilemap tm = tmGo.GetComponent<Tilemap>();
        TilemapRenderer tr = tmGo.GetComponent<TilemapRenderer>();
        tr.sortingOrder = 5;
        tr.sharedMaterial = spriteMat;

        TileBase fireTile = AssetDatabase.LoadAssetAtPath<TileBase>(FireRuleTilePath);
        if (fireTile == null)
            fireTile = AssetDatabase.LoadAssetAtPath<TileBase>(FireFallbackTilePath);

        if (fireTile == null)
        {
            Debug.LogError("[Level3Builder] Nie znaleziono kafelka FireRuleTile ani FireTileMap_1!");
            return;
        }

        void FillSolid(int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    tm.SetTile(new Vector3Int(x, y, 0), fireTile);
        }

        // Strefa 1: Przedmurze Cytadeli (x = -10..26)
        FillSolid(-10, -9, -4, 6);    // lewa sciana zamykajaca
        FillSolid(-8, 8, -4, -1);     // grunt startowy (y=0)
        FillSolid(0, 2, 0, 0);        // niski stopien (y=1)
        // x=9..11 to pierwsza rozpadlina
        FillSolid(12, 26, -4, -1);    // grunt przed klifem (y=0)

        // Strefa 2: Most Kruszacych sie Plyt (x = 27..64)
        FillSolid(27, 34, -4, 3);     // wysoki klif (y=4, wymaga BouncePad #1)
        // x=35..44: 2 kruszace sie plyty nad przepascia
        FillSolid(45, 48, -4, 3);     // srodkowy filar (y=4, z MovingHazardBall #1)
        // x=49..56: ukosna ruchoma platforma MovingPlatform #1
        FillSolid(57, 64, -4, 1);     // plaskowyz ladowania (y=2)

        // Strefa 3: Kuznia Pulapek i Zagadka ze Skrzynia (x = 65..100)
        FillSolid(65, 100, -4, -1);   // grunt glowny (y=0)
        FillSolid(73, 74, 0, 0);      // bezpieczna wysepka miedzy kolcami (y=1)
        FillSolid(91, 100, 0, 2);     // wysoki mur cytadeli (y=3, wymaga PushableCrate #1)

        // Strefa 4: Otchlan Wahadel i Podniebne Wyrzutnie (x = 101..136)
        FillSolid(103, 106, -1, 0);   // Wyspa A (y=1, z BouncePad #2)
        FillSolid(108, 113, 3, 4);    // Wysoka Wyspa B (y=5)
        // x=114..120: ruchome wahadlo MovingPlatform #2
        FillSolid(121, 124, 1, 2);    // Wyspa C (y=3, z Checkpoint #4 i BouncePad #3)
        // x=125..136: kruszace sie plyty, kula kolczasta i winda zjazdowa MovingPlatform #3

        // Strefa 5: Sanktuarium Ognia i Finalowy Oltarz (x = 137..174)
        FillSolid(137, 174, -4, -1);  // grunt glowny (y=0)
        FillSolid(137, 139, 0, 0);    // taras ladowania (y=1, z Checkpoint #5)
        FillSolid(147, 151, 0, 2);    // Skarbiec Cytadeli (y=3, wymaga PushableCrate #2)
        FillSolid(152, 153, 0, 0);    // stopien zejscia (y=1)
        FillSolid(163, 164, 0, 0);    // pierwszy stopien przed oltarzem (y=1)
        FillSolid(165, 169, 0, 1);    // podwyzszenie oltarza (y=2)
        FillSolid(170, 174, 0, 6);    // prawa sciana zamykajaca

        tm.RefreshAllTiles();
        tm.CompressBounds();
        EditorUtility.SetDirty(tm);
    }

    private static void PopulateLevel3Zones(Material spriteMat)
    {
        GameObject cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
        GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CratePrefabPath);
        GameObject scorpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScorpionPrefabPath);
        GameObject skelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath);
        GameObject violetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VioletScorpionPrefabPath);
        GameObject goldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoldScorpionPrefabPath);
        Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowSpritePath);

        Transform gCheckpoints = new GameObject("Checkpoints").transform;
        Transform gEnemies = new GameObject("Enemies").transform;
        Transform gCrates = new GameObject("PushableCrates").transform;
        Transform gCrystals = new GameObject("Crystals").transform;
        Transform gMoving = new GameObject("MovingPlatforms").transform;
        Transform gCrumbling = new GameObject("CrumblingPlatforms").transform;
        Transform gMovingHazards = new GameObject("MovingHazards").transform;
        Transform gBouncePads = new GameObject("BouncePads").transform;
        Transform gObstacles = new GameObject("LevelObstacles").transform;
        Transform gHazards = new GameObject("Hazards").transform;
        Transform gDecor = new GameObject("Decor_Level3").transform;

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
                Debug.LogWarning("[Level3Builder] Brak propu: " + propName);
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
            new Vector3(28.8f, 4.0f, 0f),
            new Vector3(66.5f, 0.0f, 0f),
            new Vector3(99.2f, 3.0f, 0f),
            new Vector3(122.0f, 3.0f, 0f),
            new Vector3(138.2f, 1.0f, 0f),
            new Vector3(161.8f, 0.0f, 0f),
        };
        for (int i = 0; i < cpPositions.Length; i++)
            Spawn(cpPrefab, gCheckpoints, cpPositions[i], "Checkpoint " + (i + 1));

        // --- 2. PRZECIWNICY (8 sztuk: 3x GoldScorpion, 2x VioletScorpion, 1x Scorpion, 2x Skeleton) ---
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

        SpawnEnemy(scorpPrefab, new Vector3(18.0f, 0.36f, 0f), 2.5f, 2.5f, 1.7f, "Scorpion 1");
        SpawnEnemy(skelPrefab, new Vector3(32.2f, 4.45f, 0f), 1.2f, 1.2f, 1.5f, "Skeleton 1");
        SpawnEnemy(violetPrefab, new Vector3(61.5f, 2.36f, 0f), 2.0f, 2.0f, 2.15f, "VioletScorpion 1");
        SpawnEnemy(goldPrefab, new Vector3(95.5f, 3.36f, 0f), 2.2f, 2.2f, 2.4f, "GoldScorpion 1");
        SpawnEnemy(violetPrefab, new Vector3(111.0f, 5.36f, 0f), 1.6f, 1.6f, 2.2f, "VioletScorpion 2");
        SpawnEnemy(goldPrefab, new Vector3(149.5f, 3.36f, 0f), 1.5f, 1.5f, 2.45f, "GoldScorpion 2");
        SpawnEnemy(goldPrefab, new Vector3(155.8f, 0.36f, 0f), 1.4f, 1.4f, 2.5f, "GoldScorpion 3");
        SpawnEnemy(skelPrefab, new Vector3(159.4f, 0.45f, 0f), 1.2f, 1.2f, 1.65f, "Skeleton 2");

        // --- 3. PRZESUWALNE SKRZYNIE ---
        Spawn(cratePrefab, gCrates, new Vector3(86.5f, 0.72f, 0f), "PushableCrate 1 (mur)");
        Spawn(cratePrefab, gCrates, new Vector3(143.0f, 0.72f, 0f), "PushableCrate 2 (skarbiec)");

        // --- 4. RUCHOME PLATFORMY ---
        void SpawnMovingPlatform(string platName, Vector3 pos, Vector2 moveOffset, float speed, float pause)
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Platform 02 X3.prefab");
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pf, gMoving);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = platName;
            go.transform.position = pos;

            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingOrder = 4;
                sr.color = new Color(1f, 0.86f, 0.76f, 1f);
            }

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

        SpawnMovingPlatform("MovingPlatform 1 (ukosna)", new Vector3(50.2f, 3.8f, 0f), new Vector2(4.0f, -1.6f), 2.4f, 0.5f);
        SpawnMovingPlatform("MovingPlatform 2 (wahadlo)", new Vector3(114.6f, 4.6f, 0f), new Vector2(3.5f, 0f), 2.5f, 0.45f);
        SpawnMovingPlatform("MovingPlatform 3 (winda zjazdowa)", new Vector3(133.2f, 4.2f, 0f), new Vector2(0f, -2.5f), 2.1f, 0.5f);

        // --- 5. KRUSZACE SIE PLATFORMY (nowa mechanika) ---
        void SpawnCrumblingPlatform(string platName, Vector3 pos)
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Platform 02 X2.prefab");
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pf, gCrumbling);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = platName;
            go.transform.position = pos;

            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingOrder = 4;

            go.AddComponent<CrumblingPlatform2D>();
        }

        SpawnCrumblingPlatform("CrumblingPlatform 1", new Vector3(36.8f, 4.0f, 0f));
        SpawnCrumblingPlatform("CrumblingPlatform 2", new Vector3(40.8f, 4.0f, 0f));
        SpawnCrumblingPlatform("CrumblingPlatform 3", new Vector3(125.8f, 5.2f, 0f));
        SpawnCrumblingPlatform("CrumblingPlatform 4", new Vector3(130.4f, 5.0f, 0f));

        // --- 6. WIRUJACE KULE Z KOLCAMI (nowa mechanika) ---
        void SpawnMovingHazardBall(string ballName, Vector3 pos, Vector2 moveOffset, float cycleSpeed, float rotSpeed, float phase)
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Spike Ball.prefab");
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(pf, gMovingHazards);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = ballName;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
                Object.DestroyImmediate(rb);

            foreach (Collider2D col in go.GetComponentsInChildren<Collider2D>(true))
                col.isTrigger = true;

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 6;
                sr.color = new Color(1f, 0.85f, 0.78f, 1f);
            }

            MovingHazardBall mhb = go.AddComponent<MovingHazardBall>();
            SerializedObject mhbSo = new SerializedObject(mhb);
            mhbSo.FindProperty("moveOffset").vector2Value = moveOffset;
            mhbSo.FindProperty("cycleSpeed").floatValue = cycleSpeed;
            mhbSo.FindProperty("rotationSpeed").floatValue = rotSpeed;
            mhbSo.FindProperty("phaseOffset").floatValue = phase;
            mhbSo.ApplyModifiedPropertiesWithoutUndo();
        }

        SpawnMovingHazardBall("MovingHazardBall 1 (filar)", new Vector3(47.0f, 4.45f, 0f), new Vector2(0f, 2.35f), 2.0f, 240f, 0f);
        SpawnMovingHazardBall("MovingHazardBall 2 (kuznia A)", new Vector3(79.5f, 0.70f, 0f), new Vector2(0f, 2.20f), 2.2f, 260f, 0f);
        SpawnMovingHazardBall("MovingHazardBall 3 (kuznia B)", new Vector3(82.5f, 0.70f, 0f), new Vector2(0f, 2.20f), 2.2f, -260f, 3.14f);
        SpawnMovingHazardBall("MovingHazardBall 4 (otchlan)", new Vector3(129.1f, 4.60f, 0f), new Vector2(0f, 2.30f), 2.0f, 250f, 1.1f);

        // --- 7. SPREZYSTE WYRZUTNIE ---
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
                    platSr.color = new Color(1f, 0.72f, 0.45f, 1f);
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
                glowSr.color = new Color(1f, 0.45f, 0.15f, 0.45f);
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

        SpawnBouncePad("BouncePad 1 (klif)", new Vector3(24.8f, 0.0f, 0f));
        SpawnBouncePad("BouncePad 2 (katapulta 1)", new Vector3(105.0f, 1.0f, 0f));
        SpawnBouncePad("BouncePad 3 (katapulta 2)", new Vector3(123.6f, 3.0f, 0f));

        // --- 8. STATYCZNE PLATFORMY I KOLCE ---
        SpawnProp("Platform 02 X3", gObstacles, new Vector3(72.2f, 2.55f, 0f), 3);
        SpawnProp("Platform 02 X3", gObstacles, new Vector3(156.2f, 2.2f, 0f), 3);

        float[] spikeXs = { 70.5f, 71.65f, 75.8f, 76.95f };
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

        // --- 9. KRYSZTALY (26 sztuk, wymagane 20) ---
        var crystalPositions = new Vector3[]
        {
            // Strefa 1 (5)
            new Vector3(1.5f, 1.85f, 0f),
            new Vector3(10.5f, 2.0f, 0f),
            new Vector3(18.0f, 1.35f, 0f),
            new Vector3(25.2f, 3.8f, 0f),
            new Vector3(26.5f, 5.1f, 0f),
            // Strefa 2 (5)
            new Vector3(37.8f, 5.0f, 0f),
            new Vector3(41.8f, 5.0f, 0f),
            new Vector3(47.0f, 5.0f, 0f),
            new Vector3(52.2f, 4.9f, 0f),
            new Vector3(61.5f, 3.3f, 0f),
            // Strefa 3 (5)
            new Vector3(71.1f, 2.1f, 0f),
            new Vector3(73.7f, 3.55f, 0f),
            new Vector3(76.4f, 2.1f, 0f),
            new Vector3(81.0f, 2.4f, 0f),
            new Vector3(95.5f, 4.3f, 0f),
            // Strefa 4 (5)
            new Vector3(106.5f, 5.2f, 0f),
            new Vector3(111.0f, 6.3f, 0f),
            new Vector3(117.5f, 5.6f, 0f),
            new Vector3(126.9f, 6.2f, 0f),
            new Vector3(131.5f, 6.0f, 0f),
            // Strefa 5 (6)
            new Vector3(134.8f, 3.5f, 0f),
            new Vector3(148.3f, 4.0f, 0f),
            new Vector3(149.5f, 4.6f, 0f),
            new Vector3(150.7f, 4.0f, 0f),
            new Vector3(157.7f, 3.2f, 0f),
            new Vector3(164.0f, 2.0f, 0f),
        };
        for (int i = 0; i < crystalPositions.Length; i++)
            Spawn(crystalPrefab, gCrystals, crystalPositions[i], "Crystal " + (i + 1));

        // --- 10. DEKORACJE W TLE (wszystkie bez colliderow) ---
        // Strefa 1
        SpawnProp("Road Sign 02", gDecor, new Vector3(-2.0f, 0f, 0f), -1, true);
        SpawnProp("Fire Bowl", gDecor, new Vector3(-6.2f, 0f, 0f), -1, true);
        SpawnProp("Brick Wall 01", gDecor, new Vector3(-0.5f, 0f, 0f), -3, true);
        SpawnProp("Campfire 01", gDecor, new Vector3(6.5f, 0f, 0f), -1, true);
        SpawnProp("Torch", gDecor, new Vector3(14.5f, 1.4f, 0f), -1, true);
        SpawnProp("Torch", gDecor, new Vector3(22.5f, 1.4f, 0f), -1, true);
        // Strefa 2
        SpawnProp("Brick Wall 01", gDecor, new Vector3(31.0f, 4.0f, 0f), -3, true);
        SpawnProp("Fire Bowl", gDecor, new Vector3(34.0f, 4.0f, 0f), -1, true);
        SpawnProp("Rock 02", gDecor, new Vector3(46.5f, 4.0f, 0f), -2, true);
        SpawnProp("Chest Iron", gDecor, new Vector3(58.8f, 2.0f, 0f), -1, true);
        SpawnProp("Barrel", gDecor, new Vector3(64.0f, 2.0f, 0f), -1, true);
        // Strefa 3
        SpawnProp("Fire Bowl", gDecor, new Vector3(68.2f, 0f, 0f), -1, true);
        SpawnProp("Gravestone 02", gDecor, new Vector3(74.0f, 1.0f, 0f), -2, true);
        SpawnProp("Torch", gDecor, new Vector3(88.5f, 1.5f, 0f), -1, true);
        SpawnProp("Brick Wall 01", gDecor, new Vector3(93.5f, 3.0f, 0f), -3, true);
        // Strefa 4
        SpawnProp("Rock 01", gDecor, new Vector3(104.0f, 1.0f, 0f), -2, true);
        SpawnProp("Gravestone 04", gDecor, new Vector3(109.2f, 5.0f, 0f), -2, true);
        SpawnProp("Road Lamp", gDecor, new Vector3(122.8f, 3.0f, 0f), -1, true);
        // Strefa 5
        SpawnProp("Rock 03", gDecor, new Vector3(139.0f, 1.0f, 0f), -2, true);
        SpawnProp("Banner", gDecor, new Vector3(141.5f, 0f, 0f), -2, true);
        SpawnProp("Chest Silver", gDecor, new Vector3(148.5f, 3.0f, 0f), -1, true);
        SpawnProp("Chest Golden", gDecor, new Vector3(150.5f, 3.0f, 0f), -1, true);
        SpawnProp("Statue", gDecor, new Vector3(153.2f, 1.0f, 0f), -2, true);
        SpawnProp("Fire Bowl", gDecor, new Vector3(165.8f, 2.0f, 0f), 4, true);
        SpawnProp("Fire Bowl", gDecor, new Vector3(168.6f, 2.0f, 0f), 4, true);
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
        gateGo.transform.position = new Vector3(167.2f, 2.0f, 0f);
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

    private static void CreateLevel3UI(
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
        GameObject hudPanel = MakePanel("HUDPanel", canvas.transform, new Color(0.14f, 0.06f, 0.07f, 0.85f), uiSprite);
        RectTransform hudRt = hudPanel.GetComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(0f, 1f);
        hudRt.pivot = new Vector2(0f, 1f);
        hudRt.anchoredPosition = new Vector2(28f, -24f);
        hudRt.sizeDelta = new Vector2(580f, 146f);

        TMP_Text levelBadge = MakeText("LevelBadge", hudPanel.transform, "POZIOM 3 • OGNISTA CYTADELA", 19f,
            new Color(1.0f, 0.74f, 0.52f, 0.95f), TextAlignmentOptions.Left, FontStyles.Bold);
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
            new Color(0.95f, 0.93f, 0.90f, 0.95f), TextAlignmentOptions.Left, FontStyles.Normal);
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
        GameObject winPanel = MakeFullscreenOverlay("WinPanel", canvas.transform, new Color(0.06f, 0.03f, 0.04f, 0.88f));
        GameObject winCard = MakeCenteredCard("Card", winPanel.transform, new Vector2(820f, 500f),
            new Color(0.18f, 0.10f, 0.12f, 0.98f), uiSprite);

        TMP_Text winTitle = MakeText("Wintitle", winCard.transform, "POZIOM 3 UKOŃCZONY!", 58f,
            new Color(0.32f, 0.95f, 0.56f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(winTitle.rectTransform, new Vector2(0f, 145f), new Vector2(760f, 100f));

        TMP_Text winSub = MakeText("WinSubtitle", winCard.transform, "Ognista Cytadela zdobyta! Ukończyłeś wszystkie poziomy!", 26f,
            new Color(0.96f, 0.94f, 0.92f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetCenteredRect(winSub.rectTransform, new Vector2(0f, 60f), new Vector2(760f, 60f));

        Button backToLvl1Btn = MakeButton("BackToLevel1Button", winCard.transform, "Wróć do Poziomu 1",
            new Vector2(0f, -45f), new Vector2(420f, 80f), new Color(0.18f, 0.72f, 0.42f, 1f), uiSprite);

        Button winRestartBtn = MakeButton("Restartbutton", winCard.transform, "Zagraj ponownie",
            new Vector2(0f, -145f), new Vector2(420f, 80f), new Color(0.24f, 0.34f, 0.52f, 1f), uiSprite);

        UnityEventTools.AddVoidPersistentListener(backToLvl1Btn.onClick, new UnityAction(levelTransition.LoadFirstLevel));
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

        GameObject deathPanel = MakeFullscreenOverlay("Panel", deathMenuGo.transform, new Color(0.06f, 0.03f, 0.04f, 0.90f));
        GameObject deathCard = MakeCenteredCard("Card", deathPanel.transform, new Vector2(780f, 540f),
            new Color(0.16f, 0.10f, 0.12f, 0.98f), uiSprite);

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

        LevelMenuBuilder.AttachInGameLevelMenuToCurrentScene("Level3");
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
        ordered.Add(new EditorBuildSettingsScene(Level3ScenePath, true));

        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing == null || string.IsNullOrEmpty(existing.path)) continue;
            if (existing.path == MainMenuScenePath || existing.path == SampleScenePath || existing.path == Level2ScenePath || existing.path == Level3ScenePath) continue;
            ordered.Add(existing);
        }

        EditorBuildSettings.scenes = ordered.ToArray();
    }

    private static void ConnectLevel2ToLevel3()
    {
        if (!File.Exists(Level2ScenePath))
        {
            Debug.LogWarning("[Level3Builder] Brak sceny Level2.unity do podpiecia przejscia.");
            return;
        }

        Scene level2Scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);

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
        ltSo.FindProperty("nextSceneName").stringValue = "Level3";
        ltSo.ApplyModifiedPropertiesWithoutUndo();

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform cardTr = canvas.transform.Find("WinPanel/Card");
            if (cardTr != null)
            {
                Transform oldBackBtn = cardTr.Find("BackToLevel1Button");
                if (oldBackBtn != null)
                    Object.DestroyImmediate(oldBackBtn.gameObject);

                Transform oldNextBtn = cardTr.Find("NextLevelButton");
                if (oldNextBtn != null)
                    Object.DestroyImmediate(oldNextBtn.gameObject);

                Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                Button nextBtn = MakeButton("NextLevelButton", cardTr, "Następny poziom",
                    new Vector2(0f, -45f), new Vector2(420f, 80f), new Color(0.18f, 0.72f, 0.42f, 1f), uiSprite);
                UnityEventTools.AddVoidPersistentListener(nextBtn.onClick, new UnityAction(levelTransition.LoadNextLevel));
            }
        }

        EditorSceneManager.MarkSceneDirty(level2Scene);
        EditorSceneManager.SaveScene(level2Scene);
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
