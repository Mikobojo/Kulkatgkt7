using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Narzedzie edytora wykonujace kompleksowy upgrade wizualny i gameplayowy sceny SampleScene.
/// </summary>
public static class LevelUpgradeBuilder
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string CrystalPrefabPath = "Assets/Prefabs/Crystal.prefab";
    private const string CheckpointPrefabPath = "Assets/Prefabs/Checkpoint.prefab";
    private const string ScorpionPrefabPath = "Assets/Prefabs/Enemies/Scorpion.prefab";
    private const string SkeletonPrefabPath = "Assets/Prefabs/Enemies/Skeleton.prefab";
    private const string CratePrefabPath = "Assets/Prefabs/PushableCrate.prefab";

    private const string CainosPropsPrefix = "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab/PF Village Props - ";
    private const string CrystalSpritePath = "Assets/Sprites/CrystalGem.png";
    private const string PlayerOrbSpritePath = "Assets/Sprites/PlayerOrb.png";
    private const string GlowSpritePath = "Assets/Sprites/SoftGlow.png";
    private const string AudioClipPath = "Assets/Audio/853656__coghezzi__sacred-rune-lock-holy-magic-seal-activation.wav";

    private const string SkyBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_01_sky.png";
    private const string MountainsBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_02_mountains.png";
    private const string HillsBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_03_hills_g.png";
    private const string SkeletonSheetPath = "Assets/Classic Legacy Pack - Village/Enemy/Skeleton.png";
    private const string ScorpionRedPrefix = "Assets/Cave Platformer Tileset/Individual PNG files/Monster/scorpion-red/scorpion-red-";

    private const string ScratchDir = @"C:\Users\mikol\.gemini\antigravity\brain\c6706ee1-9392-4b50-872b-17ea700a520f\scratch";

    private const int RequiredPoints = 16;

    [MenuItem("Tools/Upgrade Level Visuals and Gameplay")]
    public static void BuildAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        ConfigureTextures();
        Material spriteDefaultMat = ResolveSpriteMaterial();

        BuildCrystalPrefab(spriteDefaultMat);
        BuildCheckpointPrefab();
        BuildScorpionPrefab(spriteDefaultMat);
        BuildSkeletonPrefab(spriteDefaultMat);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        UpgradePlayer(spriteDefaultMat);
        UpgradeCameraAndParallax(spriteDefaultMat);
        CleanupOldSceneArtifacts();
        RebuildExtendedTilemap();
        PopulateLevelZones(spriteDefaultMat);
        UpgradeExitGate(spriteDefaultMat);
        RebuildCanvasUI();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        RenderVerificationScreenshots();
        Debug.Log("[LevelUpgradeBuilder] BuildAll completed successfully.");
    }

    private static void ConfigureTextures()
    {
        ConfigureSingleSprite(CrystalSpritePath, 32f, FilterMode.Point);
        ConfigureSingleSprite(PlayerOrbSpritePath, 32f, FilterMode.Point);
        ConfigureSingleSprite(GlowSpritePath, 32f, FilterMode.Bilinear);

        ConfigureSingleSprite(SkyBgPath, 32f, FilterMode.Point);
        ConfigureSingleSprite(MountainsBgPath, 32f, FilterMode.Point);
        ConfigureSingleSprite(HillsBgPath, 32f, FilterMode.Point);

        for (int i = 0; i < 10; i++)
        {
            ConfigureSingleSprite(ScorpionRedPrefix + i + ".png", 100f, FilterMode.Point);
        }
    }

    private static void ConfigureSingleSprite(string path, float ppu, FilterMode filterMode)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[LevelUpgradeBuilder] Missing texture at: " + path);
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

    private static void BuildCrystalPrefab(Material spriteMat)
    {
        Sprite crystalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalSpritePath);
        Sprite glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowSpritePath);

        GameObject root = new GameObject("Crystal");
        root.transform.localScale = Vector3.one;

        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = crystalSprite;
        sr.sharedMaterial = spriteMat;
        sr.color = Color.white;
        sr.sortingOrder = 6;

        CircleCollider2D col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.42f;

        root.AddComponent<CrystalPickup>();

        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        glow.transform.localScale = new Vector3(1.65f, 1.65f, 1f);
        SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = glowSprite;
        glowSr.sharedMaterial = spriteMat;
        glowSr.color = new Color(0f, 0.9f, 1f, 0.35f);
        glowSr.sortingOrder = 5;

        PrefabUtility.SaveAsPrefabAsset(root, CrystalPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildCheckpointPrefab()
    {
        GameObject bannerSrc = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Banner.prefab");
        GameObject fireBowlSrc = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Fire Bowl.prefab");

        GameObject root = new GameObject("Checkpoint");

        // Sztandar jako baza wizualna.
        GameObject banner = (GameObject)PrefabUtility.InstantiatePrefab(bannerSrc, root.transform);
        banner.name = "BannerModel";
        banner.transform.localPosition = Vector3.zero;

        SpriteRenderer bannerFlagSr = null;
        foreach (SpriteRenderer sr in banner.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingOrder = 4;
            if (sr.gameObject.name.Contains("Banner") && sr.sprite != null && sr.sprite.name.EndsWith("Banner"))
                bannerFlagSr = sr;
        }
        if (bannerFlagSr == null)
            bannerFlagSr = banner.GetComponentInChildren<SpriteRenderer>(true);

        // Znicz z ogniem obok sztandaru.
        GameObject bowl = (GameObject)PrefabUtility.InstantiatePrefab(fireBowlSrc, root.transform);
        bowl.name = "FireBowl";
        PrefabUtility.UnpackPrefabInstance(bowl, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        bowl.transform.localPosition = new Vector3(0.7f, 0f, 0f);
        bowl.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
        SpriteRenderer bowlSr = bowl.GetComponent<SpriteRenderer>();
        if (bowlSr != null)
            bowlSr.sortingOrder = 5;

        GameObject activeFlame = new GameObject("ActiveFlame");
        activeFlame.transform.SetParent(bowl.transform, false);
        activeFlame.transform.localPosition = Vector3.zero;

        // Przenies czasteczki plomienia i poswiaty do kontenera ActiveFlame.
        var toMove = new List<Transform>();
        foreach (Transform child in bowl.transform)
        {
            if (child != activeFlame.transform)
                toMove.Add(child);
        }
        foreach (Transform child in toMove)
            child.SetParent(activeFlame.transform, true);

        foreach (ParticleSystemRenderer psr in activeFlame.GetComponentsInChildren<ParticleSystemRenderer>(true))
            psr.sortingOrder = 6;

        BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.8f, 3.0f);
        trigger.offset = new Vector2(0.15f, 1.4f);

        Checkpoint cp = root.AddComponent<Checkpoint>();
        SerializedObject cpSo = new SerializedObject(cp);
        cpSo.FindProperty("flagRenderer").objectReferenceValue = bannerFlagSr;
        cpSo.FindProperty("activeEffect").objectReferenceValue = activeFlame;
        cpSo.FindProperty("spawnOffset").vector3Value = new Vector3(0f, 0.75f, 0f);
        cpSo.FindProperty("inactiveColor").colorValue = new Color(0.72f, 0.72f, 0.78f, 1f);
        cpSo.FindProperty("activeColor").colorValue = Color.white;
        cpSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, CheckpointPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildScorpionPrefab(Material spriteMat)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        var frames = new Sprite[10];
        for (int i = 0; i < 10; i++)
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(ScorpionRedPrefix + i + ".png");

        GameObject sc = new GameObject("Scorpion");
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
        pso.FindProperty("moveSpeed").floatValue = 1.6f;
        pso.FindProperty("patrolLeft").floatValue = 2.5f;
        pso.FindProperty("patrolRight").floatValue = 2.5f;
        pso.FindProperty("spriteFacesRight").boolValue = false;
        pso.FindProperty("spriteRenderer").objectReferenceValue = sr;
        pso.ApplyModifiedPropertiesWithoutUndo();

        StompableEnemy stomp = sc.AddComponent<StompableEnemy>();
        SerializedObject sso = new SerializedObject(stomp);
        sso.FindProperty("bounceVelocity").floatValue = 8.2f;
        sso.ApplyModifiedPropertiesWithoutUndo();

        SpriteFrameAnimator anim = sc.AddComponent<SpriteFrameAnimator>();
        SerializedObject aso = new SerializedObject(anim);
        SerializedProperty fp = aso.FindProperty("frames");
        fp.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            fp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        aso.FindProperty("framesPerSecond").floatValue = 9f;
        aso.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(sc, ScorpionPrefabPath);
        Object.DestroyImmediate(sc);
    }

    private static void BuildSkeletonPrefab(Material spriteMat)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(SkeletonSheetPath);
        var frames = new Sprite[3];
        foreach (Object obj in allAssets)
        {
            if (obj is Sprite sp)
            {
                if (sp.name == "Skeleton_0") frames[0] = sp;
                else if (sp.name == "Skeleton_1") frames[1] = sp;
                else if (sp.name == "Skeleton_2") frames[2] = sp;
            }
        }

        GameObject sk = new GameObject("Skeleton");
        sk.transform.localScale = new Vector3(2.8f, 2.8f, 1f);

        SpriteRenderer sr = sk.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sharedMaterial = spriteMat;
        sr.sortingOrder = 6;

        Rigidbody2D rb = sk.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        BoxCollider2D col = sk.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.16f, 0.28f);
        col.offset = new Vector2(0f, -0.01f);

        PatrolEnemy patrol = sk.AddComponent<PatrolEnemy>();
        SerializedObject pso = new SerializedObject(patrol);
        pso.FindProperty("moveSpeed").floatValue = 1.4f;
        pso.FindProperty("patrolLeft").floatValue = 2.2f;
        pso.FindProperty("patrolRight").floatValue = 2.2f;
        pso.FindProperty("spriteFacesRight").boolValue = true;
        pso.FindProperty("spriteRenderer").objectReferenceValue = sr;
        pso.ApplyModifiedPropertiesWithoutUndo();

        StompableEnemy stomp = sk.AddComponent<StompableEnemy>();
        SerializedObject sso = new SerializedObject(stomp);
        sso.FindProperty("bounceVelocity").floatValue = 8.4f;
        sso.ApplyModifiedPropertiesWithoutUndo();

        SpriteFrameAnimator anim = sk.AddComponent<SpriteFrameAnimator>();
        SerializedObject aso = new SerializedObject(anim);
        SerializedProperty fp = aso.FindProperty("frames");
        fp.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            fp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        aso.FindProperty("framesPerSecond").floatValue = 6f;
        aso.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(sk, SkeletonPrefabPath);
        Object.DestroyImmediate(sk);
    }

    private static void UpgradePlayer(Material spriteMat)
    {
        BallController ball = Object.FindFirstObjectByType<BallController>();
        if (ball == null) return;

        GameObject playerGo = ball.gameObject;
        Sprite playerOrb = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerOrbSpritePath);

        SpriteRenderer sr = playerGo.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = playerOrb;
            sr.sharedMaterial = spriteMat;
            sr.color = Color.white;
            sr.sortingOrder = 8;
        }

        playerGo.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        CircleCollider2D cc = playerGo.GetComponent<CircleCollider2D>();
        if (cc != null)
            cc.radius = 0.45f;

        ball.moveSpeed = 5.5f;
        ball.maxSpeed = 6.5f;
        ball.jumpForce = 7.2f;
        ball.groundCheckDistance = 0.18f;

        EditorUtility.SetDirty(playerGo);
    }

    private static void UpgradeCameraAndParallax(Material spriteMat)
    {
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.backgroundColor = new Color(0.45f, 0.72f, 0.92f, 1f);
            EditorUtility.SetDirty(cam.gameObject);
        }

        GameObject oldParallax = GameObject.Find("ParallaxBackground");
        if (oldParallax != null)
            Object.DestroyImmediate(oldParallax);

        GameObject root = new GameObject("ParallaxBackground");
        ParallaxBackground parallax = root.AddComponent<ParallaxBackground>();

        Sprite skySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SkyBgPath);
        Sprite mntSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MountainsBgPath);
        Sprite hillsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HillsBgPath);

        Transform skyLayer = CreateTiledLayer(root.transform, "Layer_Sky", skySprite, spriteMat, 3.5f, -30, 22f);
        Transform mntLayer = CreateTiledLayer(root.transform, "Layer_Mountains", mntSprite, spriteMat, 1.5f, -25, 22f);
        Transform hillsLayer = CreateTiledLayer(root.transform, "Layer_Hills", hillsSprite, spriteMat, -0.5f, -20, 22f);

        SerializedObject pso = new SerializedObject(parallax);
        if (cam != null)
            pso.FindProperty("targetCamera").objectReferenceValue = cam.transform;
        pso.FindProperty("verticalParallaxRatio").floatValue = 0.25f;

        SerializedProperty layersProp = pso.FindProperty("layers");
        layersProp.arraySize = 3;

        SetParallaxEntry(layersProp.GetArrayElementAtIndex(0), skyLayer, 0.95f, 22f);
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(1), mntLayer, 0.70f, 22f);
        SetParallaxEntry(layersProp.GetArrayElementAtIndex(2), hillsLayer, 0.45f, 22f);
        pso.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform CreateTiledLayer(
        Transform parent, string name, Sprite sprite, Material mat, float yPos, int sortingOrder, float tileWidth)
    {
        GameObject layerGo = new GameObject(name);
        layerGo.transform.SetParent(parent, false);
        layerGo.transform.position = new Vector3(0f, yPos, 10f);

        for (int i = -2; i <= 6; i++)
        {
            GameObject seg = new GameObject("Seg_" + (i + 2));
            seg.transform.SetParent(layerGo.transform, false);
            seg.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);

            SpriteRenderer sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
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

    private static void CleanupOldSceneArtifacts()
    {
        // Usun stare grupy, ktore odbudujemy na nowo.
        string[] oldGroups = { "Checkpoints", "Enemies", "PushableCrates", "Crystals", "Decor_Extension", "LevelObstacles", "Hazards" };
        foreach (string groupName in oldGroups)
        {
            GameObject g = GameObject.Find(groupName);
            if (g != null)
                Object.DestroyImmediate(g);
        }

        // Usun pozostale krysztaly luzem (gdyby jakies byly poza grupa).
        foreach (CrystalPickup cp in Object.FindObjectsByType<CrystalPickup>(FindObjectsSortMode.None))
            Object.DestroyImmediate(cp.gameObject);

        // Usun wiszacy kamien z nieba oraz popraw lewitujace kwiatki w starej czesci mapy.
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        var toDelete = new List<GameObject>();
        int flowerIdx = 0;

        foreach (Transform t in allTransforms)
        {
            if (t == null || t.parent != null) continue;

            if (t.name.StartsWith("PF Village Props - Stone of Recall"))
            {
                toDelete.Add(t.gameObject);
            }
            else if (t.name.StartsWith("PF Village Props - Flower") && t.position.x > 42f && t.position.y > 5.8f)
            {
                // Ustawiamy kwiatki rowno na trawie tarasu (y = 6.0) z z = 0.
                float fx = 43.3f + flowerIdx * 0.52f;
                if (fx > 46.2f) fx = 43.5f + (flowerIdx - 5) * 0.45f;
                t.position = new Vector3(fx, 6.0f, 0f);
                SpriteRenderer fsr = t.GetComponent<SpriteRenderer>();
                if (fsr != null) fsr.sortingOrder = 6;
                flowerIdx++;
            }
        }

        foreach (GameObject go in toDelete)
            Object.DestroyImmediate(go);
    }

    private static void RebuildExtendedTilemap()
    {
        Tilemap tm = Object.FindFirstObjectByType<Tilemap>();
        if (tm == null) return;

        var tiles = new Dictionary<int, TileBase>();
        foreach (Vector3Int p in tm.cellBounds.allPositionsWithin)
        {
            TileBase t = tm.GetTile(p);
            if (t == null) continue;
            string numStr = t.name.Replace("GrassTileMap_", "");
            if (int.TryParse(numStr, out int id) && !tiles.ContainsKey(id))
                tiles[id] = t;
        }

        // Wyczysc wszystko od x = 46 w prawo (kolumna 46 to prawa krawedz starej sciany).
        BoundsInt b = tm.cellBounds;
        for (int x = 47; x <= Mathf.Max(b.xMax, 150); x++)
        {
            for (int y = -8; y <= 12; y++)
                tm.SetTile(new Vector3Int(x, y, 0), null);
        }

        void Set(int x, int y, int id)
        {
            if (tiles.TryGetValue(id, out TileBase tb))
                tm.SetTile(new Vector3Int(x, y, 0), tb);
        }

        void Clear(int x, int y) => tm.SetTile(new Vector3Int(x, y, 0), null);

        void FillGround(int x0, int x1)
        {
            for (int x = x0; x <= x1; x++)
            {
                Set(x, -1, 1);
                for (int y = -2; y >= -4; y--)
                    Set(x, y, 8);
            }
        }

        void MakePit(int a, int bPit)
        {
            for (int x = a; x <= bPit; x++)
                for (int y = -1; y >= -4; y--)
                    Clear(x, y);

            Set(a - 1, -1, 2);
            for (int y = -2; y >= -4; y--) Set(a - 1, y, 9);

            Set(bPit + 1, -1, 0);
            for (int y = -2; y >= -4; y--) Set(bPit + 1, y, 7);
        }

        void MakeBlock(int x0, int x1, int topY, bool isRightWorldEdge = false)
        {
            for (int y = 0; y <= topY; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    bool top = (y == topY);
                    int id = (x == x0) ? (top ? 0 : 7) : (x == x1) ? (top ? 2 : 9) : (top ? 1 : 8);
                    Set(x, y, id);
                }
            }
            Set(x0, -1, 37);
            for (int x = x0 + 1; x < x1; x++) Set(x, -1, 8);
            Set(x1, -1, isRightWorldEdge ? 9 : 35);
        }

        void MakeThinPlatform(int x0, int x1, int y)
        {
            Set(x0, y, 25);
            for (int x = x0 + 1; x < x1; x++) Set(x, y, 26);
            Set(x1, y, 27);
        }

        // Polaczenie ze stara sciana x=46.
        Set(46, -1, 35);
        for (int y = -2; y >= -4; y--) Set(46, y, 8);

        // Strefa 2 (x=47..73) + Strefa 3 (x=74..93) + Strefa 5 (x=108..140)
        FillGround(47, 140);

        // Strefa 2: Sad i Rozpadlina
        MakeBlock(50, 51, 0);        // maly pagorek (powierzchnia y=1.0)
        MakePit(60, 62);             // 3-kafelkowa przepasc ze skocznia
        MakeBlock(66, 71, 1);        // plaskowyz (powierzchnia y=2.0)

        // Strefa 3: Ruiny i Zagadka ze Skrzynia 1
        MakeBlock(80, 84, 2);        // 3-kafelkowo wysoki taras (gora y=3.0) - wymaga skrzyni!
        MakeBlock(85, 86, 1);        // stopien zejscia (gora y=2.0)
        // Popraw laczenie miedzy blokiem 80..84 a 85..86
        Set(84, -1, 8);
        Set(84, 0, 8);
        Set(84, 1, 35);
        Set(85, -1, 8);
        Set(85, 0, 8);
        Set(85, 1, 1);

        // Strefa 4: Kanion Skokow (x=94..107)
        MakePit(94, 107);
        MakeThinPlatform(95, 96, 0);   // Wyspa 1 (gora y=1.0)
        // Mala wysepka na dole pod platforma sprezynujaca
        Set(98, -2, 0); Set(99, -2, 2);
        Set(98, -3, 7); Set(99, -3, 9);
        Set(98, -4, 7); Set(99, -4, 9);
        MakeThinPlatform(100, 102, 1); // Wyspa 2 (gora y=2.0)
        MakeThinPlatform(105, 106, 0); // Wyspa 3 (gora y=1.0)

        // Strefa 5: Twierdza Finalowa i Oltarz
        MakeBlock(117, 120, 2);      // wieza skarbow (gora y=3.0) - wymaga skrzyni 2!
        MakeBlock(132, 133, 0);      // pierwszy stopien przed oltarzem (gora y=1.0)
        MakeBlock(134, 137, 1);      // podwyzszenie oltarza (gora y=2.0)
        Set(133, -1, 8);
        Set(133, 0, 37);
        Set(134, -1, 8);
        Set(134, 0, 8);

        MakeBlock(138, 140, 4, true); // sciana zamykajaca koniec mapy
        Set(137, -1, 8);
        Set(137, 0, 8);
        Set(137, 1, 37);

        // Prawy brzeg podloza x=140
        Set(140, -1, 9);
        for (int y = -2; y >= -4; y--) Set(140, y, 9);

        tm.CompressBounds();
        EditorUtility.SetDirty(tm);
    }

    private static void PopulateLevelZones(Material spriteMat)
    {
        GameObject cpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheckpointPrefabPath);
        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath);
        GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CratePrefabPath);
        GameObject scorpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScorpionPrefabPath);
        GameObject skelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath);

        Transform gCheckpoints = new GameObject("Checkpoints").transform;
        Transform gEnemies = new GameObject("Enemies").transform;
        Transform gCrates = new GameObject("PushableCrates").transform;
        Transform gCrystals = new GameObject("Crystals").transform;
        Transform gObstacles = new GameObject("LevelObstacles").transform;
        Transform gHazards = new GameObject("Hazards").transform;
        Transform gDecor = new GameObject("Decor_Extension").transform;

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
                Debug.LogWarning("[LevelUpgradeBuilder] Prop not found: " + propName);
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

        // --- 1. CHECKPOINTY (6 sztandarow ze zniczami) ---
        var cpPositions = new Vector3[]
        {
            new Vector3(21.2f, 0f, 0f),   // Strefa 1 - po przejsciu przeszkody L
            new Vector3(31.0f, 0f, 0f),   // Strefa 1 - przed wspieciem na sciane
            new Vector3(47.8f, 0f, 0f),   // Strefa 2 - wejscie do sadu
            new Vector3(82.0f, 3f, 0f),   // Strefa 3 - szczyt tarasu za zagadka
            new Vector3(109.5f, 0f, 0f),  // Strefa 5 - za kanionem
            new Vector3(129.8f, 0f, 0f),  // Strefa 5 - przed finalem
        };
        for (int i = 0; i < cpPositions.Length; i++)
            Spawn(cpPrefab, gCheckpoints, cpPositions[i], "Checkpoint " + (i + 1));

        // --- 2. PRZECIWNICY (Skorpiony + Szkielety) ---
        void SpawnEnemy(GameObject prefab, Vector3 pos, float left, float right, float speed, string name)
        {
            GameObject go = Spawn(prefab, gEnemies, pos, name);
            PatrolEnemy pe = go.GetComponent<PatrolEnemy>();
            SerializedObject so = new SerializedObject(pe);
            so.FindProperty("patrolLeft").floatValue = left;
            so.FindProperty("patrolRight").floatValue = right;
            so.FindProperty("moveSpeed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Skorpion stoi lapami na trawie przy y = surfaceY + 0.36f, Szkielet przy y = surfaceY + 0.45f.
        SpawnEnemy(scorpPrefab, new Vector3(54.5f, 0.36f, 0f), 1.5f, 1.5f, 1.5f, "Scorpion 1");
        SpawnEnemy(skelPrefab, new Vector3(68.5f, 2.45f, 0f), 1.8f, 1.8f, 1.35f, "Skeleton 1");
        SpawnEnemy(scorpPrefab, new Vector3(89.5f, 0.36f, 0f), 2.0f, 2.0f, 1.7f, "Scorpion 2");
        SpawnEnemy(skelPrefab, new Vector3(123.5f, 0.45f, 0f), 1.8f, 1.8f, 1.45f, "Skeleton 2");
        SpawnEnemy(scorpPrefab, new Vector3(127.5f, 0.36f, 0f), 1.5f, 1.5f, 1.8f, "Scorpion 3");

        // --- 3. PRZESUWALNE SKRZYNIE ---
        Spawn(cratePrefab, gCrates, new Vector3(76.2f, 0.72f, 0f), "PushableCrate 1 (ruiny)");
        Spawn(cratePrefab, gCrates, new Vector3(113.5f, 0.72f, 0f), "PushableCrate 2 (wieza)");

        // --- 4. PLATFORMY, SKOCZNIE, SCHODY I KOLCE ---
        SpawnProp("Jump Platform 01", gObstacles, new Vector3(59.0f, 0f, 0f), 2);
        SpawnProp("Platform 01 L X1", gObstacles, new Vector3(65.3f, 1.0f, 0f), 2);
        SpawnProp("Bounding Platform 01", gObstacles, new Vector3(98.5f, -0.95f, 0f), 2);

        // Drewniana platforma nad kolcami w strefie 5
        SpawnProp("Platform 02 X3", gObstacles, new Vector3(125.5f, 1.55f, 0f), 2);

        // Kolce pod platforma w strefie 5
        for (int s = 0; s < 2; s++)
        {
            GameObject spike = SpawnProp("Spike", gHazards, new Vector3(124.8f + s * 1.15f, 0.12f, 0f), 4);
            if (spike != null)
            {
                BoxCollider2D scol = spike.GetComponent<BoxCollider2D>();
                if (scol == null) scol = spike.AddComponent<BoxCollider2D>();
                scol.isTrigger = true;
                scol.size = new Vector2(0.9f, 0.28f);
                scol.offset = new Vector2(0f, 0.08f);
                if (spike.GetComponent<HazardSpike>() == null)
                    spike.AddComponent<HazardSpike>();
            }
        }

        // Schody prowadzace na oltarz koncowy (odbite w X, zeby rosly z lewej do prawej)
        GameObject stairs = SpawnProp("Stairs X32", gObstacles, new Vector3(134.1f, 1.0f, 0f), 3);
        if (stairs != null)
            stairs.transform.localScale = new Vector3(-1.25f, 1.25f, 1f);

        // --- 5. KRYSZTALY (20 sztuk wzdłuz calej trasy) ---
        var crystalPositions = new Vector3[]
        {
            // Strefa 1 (stara mapa: 5 krysztalow)
            new Vector3(-3.2f, 0.85f, 0f),
            new Vector3(4.4f, 2.75f, 0f),
            new Vector3(20.1f, 5.75f, 0f),
            new Vector3(26.4f, 7.1f, 0f),
            new Vector3(36.5f, 6.75f, 0f),
            // Strefa 2 (5 krysztalow)
            new Vector3(51.0f, 2.15f, 0f),
            new Vector3(54.5f, 1.25f, 0f),
            new Vector3(60.6f, 2.85f, 0f),
            new Vector3(62.0f, 2.45f, 0f),
            new Vector3(69.5f, 2.85f, 0f),
            // Strefa 3 (2 krysztaly)
            new Vector3(81.5f, 3.85f, 0f),
            new Vector3(89.5f, 1.15f, 0f),
            // Strefa 4 - kanion (3 krysztaly)
            new Vector3(95.5f, 2.0f, 0f),
            new Vector3(101.0f, 3.0f, 0f),
            new Vector3(105.5f, 2.0f, 0f),
            // Strefa 5 - twierdza i final (5 krysztalow, w tym 2 bonusowe na wiezy)
            new Vector3(111.5f, 1.0f, 0f),
            new Vector3(117.8f, 3.85f, 0f),
            new Vector3(119.2f, 3.85f, 0f),
            new Vector3(125.5f, 2.55f, 0f),
            new Vector3(132.5f, 2.35f, 0f),
        };
        for (int i = 0; i < crystalPositions.Length; i++)
            Spawn(crystalPrefab, gCrystals, crystalPositions[i], "Crystal " + (i + 1));

        // --- 6. BOGATE DEKORACJE W TLE ---
        // Strefa 2
        SpawnProp("Road Sign 02", gDecor, new Vector3(47.0f, 0f, 0f), -1);
        SpawnProp("Tree 02", gDecor, new Vector3(49.5f, 0f, 0f), -3);
        SpawnProp("Fence A", gDecor, new Vector3(51.0f, 1.0f, 0f), -2);
        SpawnProp("Road Lamp", gDecor, new Vector3(53.2f, 0f, 0f), -1);
        SpawnProp("Bush 01", gDecor, new Vector3(56.0f, 0f, 0f), -1);
        SpawnProp("Grass 01", gDecor, new Vector3(63.4f, 0f, 0f), -1);
        SpawnProp("Tree 01", gDecor, new Vector3(64.5f, 0f, 0f), -3);
        SpawnProp("Chest Silver", gDecor, new Vector3(70.5f, 2.0f, 0f), -1);

        // Strefa 3
        SpawnProp("Brick Wall 01", gDecor, new Vector3(75.5f, 0f, 0f), -3, true);
        SpawnProp("Barrel", gDecor, new Vector3(74.2f, 0f, 0f), -1);
        SpawnProp("Road Lamp", gDecor, new Vector3(83.6f, 3.0f, 0f), -1);
        SpawnProp("Bush 02", gDecor, new Vector3(91.5f, 0f, 0f), -1);

        // Strefa 4
        SpawnProp("Road Lamp", gDecor, new Vector3(101.8f, 2.0f, 0f), -1);

        // Strefa 5
        SpawnProp("Sign 01", gDecor, new Vector3(108.4f, 0f, 0f), -1);
        SpawnProp("Tree 02", gDecor, new Vector3(111.2f, 0f, 0f), -3);
        SpawnProp("Chest Golden", gDecor, new Vector3(118.5f, 3.0f, 0f), -1);
        SpawnProp("Brick Wall 01", gDecor, new Vector3(123.0f, 0f, 0f), -3, true);
        SpawnProp("Road Lamp", gDecor, new Vector3(128.1f, 0f, 0f), -1);
        SpawnProp("Statue", gDecor, new Vector3(131.3f, 0f, 0f), -2);
        SpawnProp("Fire Bowl", gDecor, new Vector3(134.5f, 2.0f, 0f), 4);
        SpawnProp("Fire Bowl", gDecor, new Vector3(137.1f, 2.0f, 0f), 4);
    }

    private static void UpgradeExitGate(Material spriteMat)
    {
        ExitGate gate = Object.FindFirstObjectByType<ExitGate>();
        if (gate == null) return;

        GameObject gateGo = gate.gameObject;
        gateGo.transform.position = new Vector3(135.8f, 2.0f, 0f);
        gateGo.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

        // Pobierz sprite'y z prefabu Stone of Recall.
        GameObject stoneSrc = AssetDatabase.LoadAssetAtPath<GameObject>(CainosPropsPrefix + "Stone of Recall.prefab");
        Sprite baseSprite = stoneSrc.GetComponent<SpriteRenderer>().sprite;
        Sprite glowSprite = null;
        Transform srcGlow = stoneSrc.transform.Find("Glow");
        if (srcGlow != null)
            glowSprite = srcGlow.GetComponent<SpriteRenderer>().sprite;

        SpriteRenderer sr = gateGo.GetComponent<SpriteRenderer>();
        sr.sprite = baseSprite;
        sr.sharedMaterial = spriteMat;
        sr.color = Color.white;
        sr.sortingOrder = 5;

        BoxCollider2D col = gateGo.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
            col.size = new Vector2(0.75f, 0.9f);
            col.offset = new Vector2(0f, 0.45f);
        }

        // Dziecko RuneGlow
        Transform oldGlow = gateGo.transform.Find("RuneGlow");
        if (oldGlow != null)
            Object.DestroyImmediate(oldGlow.gameObject);

        GameObject glowGo = new GameObject("RuneGlow");
        glowGo.transform.SetParent(gateGo.transform, false);
        glowGo.transform.localPosition = srcGlow != null ? srcGlow.localPosition : new Vector3(0.06f, 0.59f, -0.01f);
        SpriteRenderer glowSr = glowGo.AddComponent<SpriteRenderer>();
        glowSr.sprite = glowSprite != null ? glowSprite : AssetDatabase.LoadAssetAtPath<Sprite>(GlowSpritePath);
        glowSr.sharedMaterial = spriteMat;
        glowSr.sortingOrder = 6;

        // AudioSource z dzwiekiem aktywacji runy
        AudioSource audio = gateGo.GetComponent<AudioSource>();
        if (audio == null) audio = gateGo.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioClipPath);

        SerializedObject gso = new SerializedObject(gate);
        gso.FindProperty("requiredPoints").intValue = RequiredPoints;
        gso.FindProperty("glowRenderer").objectReferenceValue = glowSr;
        gso.FindProperty("unlockAudio").objectReferenceValue = audio;
        gso.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RebuildCanvasUI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Wyczysc dzieci Canvasu, zeby zbudowac spójny, czysty uklad UI.
        var oldChildren = new List<GameObject>();
        foreach (Transform child in canvas.transform)
            oldChildren.Add(child.gameObject);
        foreach (GameObject go in oldChildren)
            Object.DestroyImmediate(go);

        Sprite crystalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalSpritePath);
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (mainFont != null && mainFont.fallbackFontAssetTable != null)
        {
            foreach (TMP_FontAsset fb in mainFont.fallbackFontAssetTable)
            {
                if (fb != null && fb.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                    fb.TryAddCharacters("ąęćłńóśźżĄĘĆŁŃÓŚŹŻ");
            }
        }

        // --- 1. HUD PANEL (lewy gorny rog) ---
        GameObject hudPanel = MakePanel("HUDPanel", canvas.transform, new Color(0.06f, 0.09f, 0.16f, 0.85f), uiSprite);
        RectTransform hudRt = hudPanel.GetComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(0f, 1f);
        hudRt.pivot = new Vector2(0f, 1f);
        hudRt.anchoredPosition = new Vector2(28f, -24f);
        hudRt.sizeDelta = new Vector2(560f, 118f);

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
        scoreRt.anchorMin = new Vector2(0f, 0.5f);
        scoreRt.anchorMax = new Vector2(1f, 1f);
        scoreRt.offsetMin = new Vector2(98f, 4f);
        scoreRt.offsetMax = new Vector2(-18f, -12f);

        TMP_Text goalText = MakeText("Goaltext", hudPanel.transform, "Cel: Zbierz " + RequiredPoints + " kryształów (0/" + RequiredPoints + ")", 21f,
            new Color(0.92f, 0.95f, 1.0f, 0.95f), TextAlignmentOptions.Left, FontStyles.Normal);
        RectTransform goalRt = goalText.rectTransform;
        goalRt.anchorMin = new Vector2(0f, 0f);
        goalRt.anchorMax = new Vector2(1f, 0.5f);
        goalRt.offsetMin = new Vector2(98f, 12f);
        goalRt.offsetMax = new Vector2(-18f, -2f);

        // Podlacz ScoreDisplay na Canvasie.
        ScoreDisplay scoreDisplay = canvas.GetComponent<ScoreDisplay>();
        if (scoreDisplay == null) scoreDisplay = canvas.gameObject.AddComponent<ScoreDisplay>();
        BallController player = Object.FindFirstObjectByType<BallController>();
        ExitGate exitGate = Object.FindFirstObjectByType<ExitGate>();

        SerializedObject sdSo = new SerializedObject(scoreDisplay);
        sdSo.FindProperty("player").objectReferenceValue = player;
        sdSo.FindProperty("exitGate").objectReferenceValue = exitGate;
        sdSo.FindProperty("scoreText").objectReferenceValue = scoreText;
        sdSo.ApplyModifiedPropertiesWithoutUndo();

        // --- 2. WIN PANEL ---
        GameObject winPanel = MakeFullscreenOverlay("WinPanel", canvas.transform, new Color(0.03f, 0.06f, 0.10f, 0.88f));
        GameObject winCard = MakeCenteredCard("Card", winPanel.transform, new Vector2(780f, 440f),
            new Color(0.10f, 0.16f, 0.25f, 0.98f), uiSprite);

        TMP_Text winTitle = MakeText("Wintitle", winCard.transform, "POZIOM UKOŃCZONY!", 64f,
            new Color(0.32f, 0.95f, 0.56f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(winTitle.rectTransform, new Vector2(0f, 105f), new Vector2(700f, 100f));

        TMP_Text winSub = MakeText("WinSubtitle", winCard.transform, "Ołtarz runiczny został aktywowany!", 30f,
            new Color(0.92f, 0.96f, 1f, 1f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetCenteredRect(winSub.rectTransform, new Vector2(0f, 15f), new Vector2(680f, 60f));

        Button winRestartBtn = MakeButton("Restartbutton", winCard.transform, "Zagraj ponownie",
            new Vector2(0f, -105f), new Vector2(400f, 84f), new Color(0.18f, 0.72f, 0.42f, 1f), uiSprite);

        LevelRestart levelRestart = Object.FindFirstObjectByType<LevelRestart>();
        if (levelRestart != null)
            UnityEventTools.AddVoidPersistentListener(winRestartBtn.onClick, new UnityAction(levelRestart.RestartLevel));

        winPanel.SetActive(false);

        if (exitGate != null)
        {
            SerializedObject egSo = new SerializedObject(exitGate);
            egSo.FindProperty("player").objectReferenceValue = player;
            egSo.FindProperty("goalText").objectReferenceValue = goalText;
            egSo.FindProperty("winPanel").objectReferenceValue = winPanel;
            egSo.FindProperty("hudRoot").objectReferenceValue = hudPanel;
            egSo.ApplyModifiedPropertiesWithoutUndo();
        }

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

        PlayerDeath playerDeath = Object.FindFirstObjectByType<PlayerDeath>();
        SerializedObject dmSo = new SerializedObject(deathMenu);
        dmSo.FindProperty("panel").objectReferenceValue = deathPanel;
        dmSo.FindProperty("hudRoot").objectReferenceValue = hudPanel;
        dmSo.FindProperty("reasonText").objectReferenceValue = dReason;
        dmSo.FindProperty("scoreText").objectReferenceValue = dScore;
        dmSo.FindProperty("player").objectReferenceValue = playerDeath;
        dmSo.FindProperty("levelRestart").objectReferenceValue = levelRestart;
        dmSo.ApplyModifiedPropertiesWithoutUndo();

        if (playerDeath != null)
        {
            SerializedObject pdSo = new SerializedObject(playerDeath);
            pdSo.FindProperty("deathMenu").objectReferenceValue = deathMenu;
            pdSo.ApplyModifiedPropertiesWithoutUndo();
        }

        deathPanel.SetActive(false);
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
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
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

    private static void RenderVerificationScreenshots()
    {
        Directory.CreateDirectory(ScratchDir);

        // Strefy 1-2: x = -8..70
        RenderCamRect("upgraded_zone1_2.png", new Vector3(31f, 3.5f, -10f), 78f, 14f, 1950, 350);
        // Strefy 3-5: x = 68..142
        RenderCamRect("upgraded_zone3_5.png", new Vector3(105f, 3.5f, -10f), 74f, 14f, 1850, 350);

        // Zblizenia (gracz+krysztal, skorpion+checkpoint, szkielet, oltarz wyjscia)
        Texture2D[] shots = new Texture2D[4];

        shots[0] = CaptureRectTexture(new Vector3(-4.0f, 1.0f, -10f), 8f, 5f, 400, 250);
        shots[1] = CaptureRectTexture(new Vector3(54.5f, 1.3f, -10f), 9f, 5f, 450, 250);
        shots[2] = CaptureRectTexture(new Vector3(68.5f, 2.8f, -10f), 8f, 5f, 400, 250);
        shots[3] = CaptureRectTexture(new Vector3(135.5f, 3.2f, -10f), 9f, 5f, 450, 250);

        int totalW = 400 + 450 + 400 + 450;
        Texture2D combined = new Texture2D(totalW, 250, TextureFormat.RGB24, false);
        int xOff = 0;
        for (int i = 0; i < shots.Length; i++)
        {
            combined.SetPixels(xOff, 0, shots[i].width, shots[i].height, shots[i].GetPixels());
            xOff += shots[i].width;
            Object.DestroyImmediate(shots[i]);
        }
        combined.Apply();
        File.WriteAllBytes(Path.Combine(ScratchDir, "upgraded_closeups.png"), combined.EncodeToPNG());
        Object.DestroyImmediate(combined);

        RenderUIPreviewScreenshots();
    }

    private static void RenderUIPreviewScreenshots()
    {
        // Tymczasowo przestawiamy Canvas na ScreenSpaceCamera, zeby zrzucic podglad HUD i menu.
        // Scena zostala juz zapisana wczesniej, wiec po wykonaniu zrzutu nie nadpisujemy pliku .unity.
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject uiCamGo = new GameObject("TmpUICam");
        Camera uiCam = uiCamGo.AddComponent<Camera>();
        uiCam.orthographic = true;
        uiCam.orthographicSize = 4.5f;
        uiCam.aspect = 16f / 9f;
        uiCam.backgroundColor = new Color(0.45f, 0.72f, 0.92f, 1f);
        uiCam.clearFlags = CameraClearFlags.SolidColor;
        uiCamGo.transform.position = new Vector3(-3.5f, 1.5f, -10f);

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCam;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 100;

        GameObject hud = canvas.transform.Find("HUDPanel")?.gameObject;
        GameObject win = canvas.transform.Find("WinPanel")?.gameObject;
        GameObject death = canvas.transform.Find("DeathMenu/Panel")?.gameObject;

        // 1. Widok z HUDem
        if (hud != null) hud.SetActive(true);
        if (win != null) win.SetActive(false);
        if (death != null) death.SetActive(false);
        Canvas.ForceUpdateCanvases();
        SaveCameraRender(uiCam, 960, 540, "upgraded_hud.png");

        // 2. Widok z DeathMenu
        if (hud != null) hud.SetActive(false);
        if (death != null) death.SetActive(true);
        Canvas.ForceUpdateCanvases();
        SaveCameraRender(uiCam, 960, 540, "upgraded_deathmenu.png");

        // 3. Widok z WinPanel
        if (death != null) death.SetActive(false);
        if (win != null) win.SetActive(true);
        Canvas.ForceUpdateCanvases();
        SaveCameraRender(uiCam, 960, 540, "upgraded_winpanel.png");

        Object.DestroyImmediate(uiCamGo);
    }

    private static void SaveCameraRender(Camera c, int pw, int ph, string fileName)
    {
        RenderTexture rt = new RenderTexture(pw, ph, 24);
        c.targetTexture = rt;
        c.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        c.targetTexture = null;
        File.WriteAllBytes(Path.Combine(ScratchDir, fileName), tex.EncodeToPNG());
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    private static void RenderCamRect(string fileName, Vector3 pos, float worldW, float worldH, int pw, int ph)
    {
        Texture2D tex = CaptureRectTexture(pos, worldW, worldH, pw, ph);
        File.WriteAllBytes(Path.Combine(ScratchDir, fileName), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static Texture2D CaptureRectTexture(Vector3 pos, float worldW, float worldH, int pw, int ph)
    {
        GameObject go = new GameObject("TmpVerifyCam");
        Camera c = go.AddComponent<Camera>();
        c.orthographic = true;
        c.backgroundColor = new Color(0.45f, 0.72f, 0.92f, 1f);
        c.clearFlags = CameraClearFlags.SolidColor;
        c.aspect = worldW / worldH;
        c.orthographicSize = worldH / 2f;
        go.transform.position = pos;

        RenderTexture rt = new RenderTexture(pw, ph, 24);
        c.targetTexture = rt;
        c.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        c.targetTexture = null;

        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
        return tex;
    }
}
