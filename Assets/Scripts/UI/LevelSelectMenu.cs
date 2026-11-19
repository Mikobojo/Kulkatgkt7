using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Glowne menu wyboru poziomow (Poziom 1, Poziom 2, Poziom 3).
/// Obsluguje przyciski UI oraz szybki start klawiszami 1, 2 i 3.
/// </summary>
public class LevelSelectMenu : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            PlayLevel("SampleScene");
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            PlayLevel("Level2");
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            PlayLevel("Level3");
    }

    public void PlayLevel(string sceneName)
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Debug.LogWarning("[LevelSelectMenu] Scena '" + sceneName + "' nie znajduje sie w Build Settings.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
