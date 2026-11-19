using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Obsluguje przejscie do kolejnego poziomu, powrot do Poziomu 1, menu glownego lub wybranej sceny po nazwie.
/// </summary>
public class LevelTransition : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "";

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
                return;
            }

            Debug.LogWarning("[LevelTransition] Scena '" + nextSceneName + "' nie jest dodana do Build Settings.");
        }

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= 0 && nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
            return;
        }

        Debug.LogWarning("[LevelTransition] Brak kolejnej sceny w Build Settings.");
    }

    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Debug.LogWarning("[LevelTransition] Nie mozna zaladowac sceny '" + sceneName + "'. Sprawdz Build Settings.");
    }

    public void LoadFirstLevel()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded("SampleScene"))
        {
            SceneManager.LoadScene("SampleScene");
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
            return;
        }

        Debug.LogWarning("[LevelTransition] Brak scen w Build Settings.");
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
            return;
        }

        Debug.LogWarning("[LevelTransition] Brak sceny MainMenu w Build Settings.");
    }

    public bool HasNextLevel()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
            return true;

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        return nextIndex >= 0 && nextIndex < SceneManager.sceneCountInBuildSettings;
    }
}
