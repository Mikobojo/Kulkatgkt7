using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Narzedzie edytora do budowania wersji przegladarkowej gry (WebGL lub fallback HTML5)
/// oraz uruchamiania jej w Google Chrome pod lokalnym serwerem HTTP.
/// </summary>
public static class WebGLBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string Level2ScenePath = "Assets/Scenes/Level2.unity";
    private const string Level3ScenePath = "Assets/Scenes/Level3.unity";
    private const string WebGLOutputDir = "Builds/WebGL";
    private const string WebGLUrl = "http://localhost:8085/Builds/WebGL/index.html";
    private const string BrowserFallbackUrl = "http://localhost:8085/Builds/Browser/index.html";
    private const string ChromeExePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    private static bool pendingAfterPlayMode;

    [MenuItem("Tools/Build and Run WebGL (Chrome)")]
    public static void BuildAndRunWebGL()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!pendingAfterPlayMode)
            {
                pendingAfterPlayMode = true;
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }
            EditorApplication.isPlaying = false;
            return;
        }

        BuildInternal();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        pendingAfterPlayMode = false;
        EditorApplication.delayCall += BuildInternal;
    }

    private static void BuildInternal()
    {
        if (!File.Exists(Level3ScenePath))
            Level3Builder.BuildAll();

        if (!File.Exists(MainMenuScenePath))
            LevelMenuBuilder.BuildAll();

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            Debug.LogWarning(
                "[WebGLBuilder] Moduł WebGL Build Support nie jest zainstalowany w Unity Hub dla wersji " +
                Application.unityVersion + ". Otwieram gotowy build przeglądarkowy: " + BrowserFallbackUrl);
            OpenInChrome(BrowserFallbackUrl);
            return;
        }

        Directory.CreateDirectory(WebGLOutputDir);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        var sceneList = new List<string>();
        if (File.Exists(MainMenuScenePath))
            sceneList.Add(MainMenuScenePath);
        sceneList.Add(SampleScenePath);
        sceneList.Add(Level2ScenePath);
        sceneList.Add(Level3ScenePath);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = sceneList.ToArray(),
            locationPathName = WebGLOutputDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[WebGLBuilder] Build WebGL zakończony pomyślnie: " + WebGLOutputDir);
            OpenInChrome(WebGLUrl);
        }
        else
        {
            Debug.LogError("[WebGLBuilder] Build WebGL nie powiódł się: " + report.summary.result);
        }
    }

    private static void OpenInChrome(string url)
    {
        if (File.Exists(ChromeExePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ChromeExePath,
                Arguments = url,
                UseShellExecute = true
            });
            return;
        }

        Application.OpenURL(url);
    }
}
