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
using UnityEngine.UI;

/// <summary>
/// Narzedzie edytora tworzace scene menu wyboru poziomow (Assets/Scenes/MainMenu.unity),
/// dodajace podreczne menu przelaczania poziomow (Poziomy / ESC) do wszystkich istniejacych poziomow
/// oraz ustawiajace kolejnosc scen w Build Settings.
/// </summary>
public static class LevelMenuBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string Level2ScenePath = "Assets/Scenes/Level2.unity";
    private const string Level3ScenePath = "Assets/Scenes/Level3.unity";

    private const string CrystalPrefabPath = "Assets/Prefabs/Crystal.prefab";
    private const string CrystalSpritePath = "Assets/Sprites/CrystalGem.png";
    private const string PlayerOrbSpritePath = "Assets/Sprites/PlayerOrb.png";

    private const string SkyBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_01_sky.png";
    private const string MountainsBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_02_mountains.png";
    private const string HillsBgPath = "Assets/Cave Platformer Tileset/Example stage layers/bg_03_hills_g.png";

    private const string RedScorpionSpritePath = "Assets/Cave Platformer Tileset/Individual PNG files/Monster/scorpion-red/scorpion-red-0.png";
    private const string VioletScorpionSpritePath = "Assets/Cave Platformer Tileset/Individual PNG files/Monster/scorpion-violet/scorpion-violet-0.png";
    private const string GoldScorpionSpritePath = "Assets/Cave Platformer Tileset/Individual PNG files/Monster/scorpion-gold/scorpion-gold-0.png";
    private const string MainFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    private static bool pendingBuildAfterPlayMode;

    [InitializeOnLoadMethod]
    private static void AutoBuildIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(MainMenuScenePath))
                return;

            BuildAll();
        };
    }

    [MenuItem("Tools/Build Level Select Menu")]
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

        Material spriteMat = ResolveSpriteMaterial();

        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildMainMenuSceneContents(spriteMat);
        EditorSceneManager.MarkSceneDirty(menuScene);
        EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);

        UpdateBuildSettings();

        AttachMenuToSceneIfExists("SampleScene", SampleScenePath);
        AttachMenuToSceneIfExists("Level2", Level2ScenePath);
        AttachMenuToSceneIfExists("Level3", Level3ScenePath);

        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();

        Debug.Log("[LevelMenuBuilder] Scena MainMenu.unity oraz menu poziomow we wszystkich etapach zostaly zbudowane pomyslnie!");
    }

    private static void AttachMenuToSceneIfExists(string sceneName, string scenePath)
    {
        if (!File.Exists(scenePath))
            return;

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        AttachInGameLevelMenuToCurrentScene(sceneName);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Dodaje (lub odswieza idempotentnie) przycisk "Poziomy (ESC)" oraz nakladke wyboru poziomow
    /// w Canvasie aktualnie otwartej sceny poziomu.
    /// </summary>
    public static void AttachInGameLevelMenuToCurrentScene(string currentSceneName)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("InGameLevelMenu");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        EnsurePolishCharacters();
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject rootGo = new GameObject("InGameLevelMenu", typeof(RectTransform));
        rootGo.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = rootGo.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        rootGo.transform.SetAsLastSibling();

        InGameLevelMenu menu = rootGo.AddComponent<InGameLevelMenu>();

        // Przycisk w prawym gornym rogu ekranu
        Button openBtn = MakeButton("OpenMenuButton", rootGo.transform, "Poziomy (ESC)",
            Vector2.zero, new Vector2(220f, 58f), new Color(0.08f, 0.13f, 0.22f, 0.88f), uiSprite, 22f);
        RectTransform openRt = openBtn.GetComponent<RectTransform>();
        openRt.anchorMin = new Vector2(1f, 1f);
        openRt.anchorMax = new Vector2(1f, 1f);
        openRt.pivot = new Vector2(1f, 1f);
        openRt.anchoredPosition = new Vector2(-28f, -24f);
        UnityEventTools.AddVoidPersistentListener(openBtn.onClick, new UnityAction(menu.ToggleMenu));

        // Pelnoekranowa nakladka wyboru poziomow
        GameObject overlay = MakeFullscreenOverlay("OverlayPanel", rootGo.transform, new Color(0.03f, 0.05f, 0.10f, 0.86f));
        GameObject card = MakeCenteredCard("Card", overlay.transform, new Vector2(720f, 630f),
            new Color(0.09f, 0.14f, 0.23f, 0.98f), uiSprite);

        TMP_Text title = MakeText("Title", card.transform, "WYBÓR POZIOMU", 50f,
            new Color(0.35f, 0.95f, 1.0f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(title.rectTransform, new Vector2(0f, 240f), new Vector2(660f, 70f));

        TMP_Text subtitle = MakeText("Subtitle", card.transform, "Wybierz etap, do którego chcesz przejść", 24f,
            new Color(0.86f, 0.91f, 0.98f, 0.95f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetCenteredRect(subtitle.rectTransform, new Vector2(0f, 182f), new Vector2(660f, 44f));

        AddInGameLevelButton(card.transform, menu, "SampleScene", currentSceneName,
            "Poziom 1 • Słoneczna Dolina", new Vector2(0f, 95f),
            new Color(0.16f, 0.62f, 0.42f, 1f), new Color(0.16f, 0.26f, 0.36f, 1f), uiSprite);

        AddInGameLevelButton(card.transform, menu, "Level2", currentSceneName,
            "Poziom 2 • Kamienna Twierdza", new Vector2(0f, 10f),
            new Color(0.18f, 0.56f, 0.80f, 1f), new Color(0.16f, 0.26f, 0.36f, 1f), uiSprite);

        AddInGameLevelButton(card.transform, menu, "Level3", currentSceneName,
            "Poziom 3 • Ognista Cytadela", new Vector2(0f, -75f),
            new Color(0.76f, 0.34f, 0.20f, 1f), new Color(0.16f, 0.26f, 0.36f, 1f), uiSprite);

        Button mainMenuBtn = MakeButton("MainMenuButton", card.transform, "Menu główne",
            new Vector2(0f, -165f), new Vector2(540f, 68f), new Color(0.24f, 0.30f, 0.44f, 1f), uiSprite, 27f);
        UnityEventTools.AddStringPersistentListener(mainMenuBtn.onClick, new UnityAction<string>(menu.LoadLevel), "MainMenu");

        Button resumeBtn = MakeButton("ResumeButton", card.transform, "Wznów grę",
            new Vector2(0f, -248f), new Vector2(540f, 68f), new Color(0.18f, 0.68f, 0.42f, 1f), uiSprite, 28f);
        UnityEventTools.AddVoidPersistentListener(resumeBtn.onClick, new UnityAction(menu.CloseMenu));

        SerializedObject menuSo = new SerializedObject(menu);
        menuSo.FindProperty("overlayPanel").objectReferenceValue = overlay;
        menuSo.FindProperty("player").objectReferenceValue = Object.FindFirstObjectByType<BallController>();
        menuSo.FindProperty("playerDeath").objectReferenceValue = Object.FindFirstObjectByType<PlayerDeath>();
        menuSo.ApplyModifiedPropertiesWithoutUndo();

        overlay.SetActive(false);
        EditorUtility.SetDirty(canvas.gameObject);
    }

    private static void AddInGameLevelButton(
        Transform parent,
        InGameLevelMenu menu,
        string targetScene,
        string currentScene,
        string label,
        Vector2 pos,
        Color activeColor,
        Color defaultColor,
        Sprite uiSprite)
    {
        bool isCurrent = string.Equals(targetScene, currentScene, System.StringComparison.OrdinalIgnoreCase);
        string text = isCurrent ? label + " (aktualny)" : label;
        Color bg = isCurrent ? activeColor : defaultColor;

        Button btn = MakeButton("LevelBtn_" + targetScene, parent, text, pos, new Vector2(540f, 70f), bg, uiSprite, 26f);
        UnityEventTools.AddStringPersistentListener(btn.onClick, new UnityAction<string>(menu.LoadLevel), targetScene);
    }

    private static void BuildMainMenuSceneContents(Material spriteMat)
    {
        EnsurePolishCharacters();

        // 1. Kamera
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0.5f, -10f);

        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.26f, 0.44f, 1f);

        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();

        // 2. Dekoracyjne warstwy tla z istniejacych zasobow gry
        GameObject backdropGo = new GameObject("MenuBackdrop");
        CreateMenuBackdropLayer(backdropGo.transform, "Layer_Sky", AssetDatabase.LoadAssetAtPath<Sprite>(SkyBgPath),
            spriteMat, 2.2f, -30, 22f, new Color(0.60f, 0.72f, 0.90f, 1f));
        CreateMenuBackdropLayer(backdropGo.transform, "Layer_Mountains", AssetDatabase.LoadAssetAtPath<Sprite>(MountainsBgPath),
            spriteMat, 0.5f, -25, 22f, new Color(0.65f, 0.76f, 0.92f, 1f));
        CreateMenuBackdropLayer(backdropGo.transform, "Layer_Hills", AssetDatabase.LoadAssetAtPath<Sprite>(HillsBgPath),
            spriteMat, -1.2f, -20, 22f, new Color(0.68f, 0.78f, 0.88f, 1f));

        // 3. Canvas z 3 kartami poziomow
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        LevelSelectMenu menu = canvasGo.AddComponent<LevelSelectMenu>();
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        MakeFullscreenOverlay("BackdropOverlay", canvas.transform, new Color(0.04f, 0.07f, 0.14f, 0.56f));

        // Naglowek z ikonami kulki i krysztalu
        CreateCenteredIcon("PlayerIcon", canvas.transform, AssetDatabase.LoadAssetAtPath<Sprite>(PlayerOrbSpritePath),
            new Vector2(-420f, 360f), new Vector2(82f, 82f));
        CreateCenteredIcon("CrystalIcon", canvas.transform, AssetDatabase.LoadAssetAtPath<Sprite>(CrystalSpritePath),
            new Vector2(420f, 360f), new Vector2(82f, 82f));

        TMP_Text mainTitle = MakeText("MainTitle", canvas.transform, "PRZYGODA KULKI", 72f,
            new Color(0.45f, 0.96f, 1.0f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(mainTitle.rectTransform, new Vector2(0f, 360f), new Vector2(760f, 100f));

        TMP_Text subTitle = MakeText("SubTitle", canvas.transform, "Wybierz poziom, który chcesz przejść", 30f,
            new Color(0.92f, 0.95f, 1.0f, 0.95f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetCenteredRect(subTitle.rectTransform, new Vector2(0f, 280f), new Vector2(820f, 54f));

        // 3 karty poziomow
        CreateLevelCard(
            canvas.transform, menu, uiSprite,
            new Vector2(-520f, -20f),
            new Color(0.22f, 0.84f, 0.52f, 1f),
            new Color(0.18f, 0.72f, 0.42f, 1f),
            "POZIOM 1 • ŁATWY",
            "Słoneczna Dolina",
            AssetDatabase.LoadAssetAtPath<Sprite>(RedScorpionSpritePath),
            "Cel: 16 / 20 kryształów\nSkorpiony • Szkielety\nSkrzynie i skocznie",
            "SampleScene");

        CreateLevelCard(
            canvas.transform, menu, uiSprite,
            new Vector2(0f, -20f),
            new Color(0.32f, 0.78f, 1.0f, 1f),
            new Color(0.18f, 0.58f, 0.86f, 1f),
            "POZIOM 2 • ŚREDNI",
            "Kamienna Twierdza",
            AssetDatabase.LoadAssetAtPath<Sprite>(VioletScorpionSpritePath),
            "Cel: 18 / 24 kryształów\nRuchome platformy\nWyrzutnie • Fioletowe skorpiony",
            "Level2");

        CreateLevelCard(
            canvas.transform, menu, uiSprite,
            new Vector2(520f, -20f),
            new Color(1.0f, 0.48f, 0.24f, 1f),
            new Color(0.84f, 0.34f, 0.18f, 1f),
            "POZIOM 3 • TRUDNY",
            "Ognista Cytadela",
            AssetDatabase.LoadAssetAtPath<Sprite>(GoldScorpionSpritePath),
            "Cel: 20 / 26 kryształów\nKruszące platformy\nKolczaste kule • Złote skorpiony",
            "Level3");

        // Stopka sterowania
        TMP_Text footer = MakeText("ControlsHint", canvas.transform,
            "Sterowanie: A / D lub strzałki — ruch   •   Spacja / W / ↑ — skok   •   ESC — menu poziomów   •   Klawisze 1–3 — szybki start",
            22f, new Color(0.84f, 0.90f, 0.98f, 0.90f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetCenteredRect(footer.rectTransform, new Vector2(0f, -365f), new Vector2(1560f, 50f));

        // 4. EventSystem
        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreateMenuBackdropLayer(
        Transform parent, string name, Sprite sprite, Material mat, float yPos, int sortingOrder, float tileWidth, Color tint)
    {
        if (sprite == null)
            return;

        GameObject layerGo = new GameObject(name);
        layerGo.transform.SetParent(parent, false);
        layerGo.transform.position = new Vector3(0f, yPos, 10f);

        for (int i = -1; i <= 1; i++)
        {
            GameObject seg = new GameObject("Seg_" + (i + 1));
            seg.transform.SetParent(layerGo.transform, false);
            seg.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);

            SpriteRenderer sr = seg.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.color = tint;
            sr.sortingOrder = sortingOrder;
        }
    }

    private static void CreateLevelCard(
        Transform canvasTransform,
        LevelSelectMenu menu,
        Sprite uiSprite,
        Vector2 pos,
        Color accentColor,
        Color buttonColor,
        string badgeText,
        string titleText,
        Sprite previewSprite,
        string descText,
        string targetScene)
    {
        GameObject card = MakePanel("LevelCard_" + targetScene, canvasTransform, new Color(0.08f, 0.12f, 0.21f, 0.95f), uiSprite);
        SetCenteredRect(card.GetComponent<RectTransform>(), pos, new Vector2(470f, 510f));

        // Kolorowy pasek akcentu u gory karty
        GameObject bar = new GameObject("AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(card.transform, false);
        RectTransform barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 1f);
        barRt.anchorMax = new Vector2(1f, 1f);
        barRt.pivot = new Vector2(0.5f, 1f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(0f, 12f);
        Image barImg = bar.GetComponent<Image>();
        barImg.color = accentColor;
        barImg.raycastTarget = false;

        TMP_Text badge = MakeText("Badge", card.transform, badgeText, 22f, accentColor, TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(badge.rectTransform, new Vector2(0f, 200f), new Vector2(420f, 40f));

        TMP_Text title = MakeText("Title", card.transform, titleText, 38f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        SetCenteredRect(title.rectTransform, new Vector2(0f, 145f), new Vector2(430f, 56f));

        if (previewSprite != null)
            CreateCenteredIcon("PreviewIcon", card.transform, previewSprite, new Vector2(0f, 52f), new Vector2(96f, 96f));

        TMP_Text desc = MakeText("Description", card.transform, descText, 23f,
            new Color(0.86f, 0.91f, 0.98f, 0.95f), TextAlignmentOptions.Center, FontStyles.Normal);
        SetCenteredRect(desc.rectTransform, new Vector2(0f, -64f), new Vector2(420f, 118f));

        Button playBtn = MakeButton("PlayButton", card.transform, "ZAGRAJ",
            new Vector2(0f, -182f), new Vector2(360f, 76f), buttonColor, uiSprite, 32f);
        UnityEventTools.AddStringPersistentListener(playBtn.onClick, new UnityAction<string>(menu.PlayLevel), targetScene);
    }

    private static void CreateCenteredIcon(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        if (sprite == null)
            return;

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        SetCenteredRect(go.GetComponent<RectTransform>(), pos, size);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    private static void UpdateBuildSettings()
    {
        var ordered = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true)
        };

        if (File.Exists(SampleScenePath))
            ordered.Add(new EditorBuildSettingsScene(SampleScenePath, true));
        if (File.Exists(Level2ScenePath))
            ordered.Add(new EditorBuildSettingsScene(Level2ScenePath, true));
        if (File.Exists(Level3ScenePath))
            ordered.Add(new EditorBuildSettingsScene(Level3ScenePath, true));

        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing == null || string.IsNullOrEmpty(existing.path)) continue;
            if (existing.path == MainMenuScenePath ||
                existing.path == SampleScenePath ||
                existing.path == Level2ScenePath ||
                existing.path == Level3ScenePath)
                continue;

            ordered.Add(existing);
        }

        EditorBuildSettings.scenes = ordered.ToArray();
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

    private static void EnsurePolishCharacters()
    {
        TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
        if (mainFont == null || mainFont.fallbackFontAssetTable == null)
            return;

        foreach (TMP_FontAsset fb in mainFont.fallbackFontAssetTable)
        {
            if (fb != null && fb.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                fb.TryAddCharacters("ąęćłńóśźżĄĘĆŁŃÓŚŹŻ");
        }
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
        string name, Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor, Sprite sprite, float fontSize = 30f)
    {
        GameObject go = MakePanel(name, parent, bgColor, sprite);
        SetCenteredRect(go.GetComponent<RectTransform>(), pos, size);
        Button btn = go.AddComponent<Button>();

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = colors;

        TMP_Text txt = MakeText("Label", go.transform, label, fontSize, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
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
