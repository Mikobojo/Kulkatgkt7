using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Podreczne menu wyboru poziomow dostepne podczas rozgrywki (przycisk w rogu ekranu lub klawisz ESC).
/// Pauzuje czas gry i bezpiecznie blokuje sterowanie kulka na czas wyboru poziomu.
/// </summary>
public class InGameLevelMenu : MonoBehaviour
{
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private BallController player;
    [SerializeField] private PlayerDeath playerDeath;
    [SerializeField] private bool allowEscapeToggle = true;

    private bool isOpen;
    private bool wasBallEnabled;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<BallController>();

        if (playerDeath == null)
            playerDeath = FindFirstObjectByType<PlayerDeath>();

        if (overlayPanel != null)
            overlayPanel.SetActive(false);
    }

    private void Update()
    {
        if (allowEscapeToggle && Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (isOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        if (isOpen) return;

        isOpen = true;
        wasBallEnabled = player != null && player.enabled;
        if (wasBallEnabled)
            player.enabled = false;

        Time.timeScale = 0f;

        if (overlayPanel != null)
            overlayPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        if (!isOpen) return;

        isOpen = false;
        Time.timeScale = 1f;

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        if (wasBallEnabled && player != null && (playerDeath == null || !playerDeath.IsDead))
            player.enabled = true;

        wasBallEnabled = false;
    }

    public void LoadLevel(string sceneName)
    {
        isOpen = false;
        wasBallEnabled = false;
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Debug.LogWarning("[InGameLevelMenu] Scena '" + sceneName + "' nie znajduje sie w Build Settings.");
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            Time.timeScale = 1f;
            isOpen = false;
        }
    }

    private void OnDestroy()
    {
        if (isOpen)
        {
            Time.timeScale = 1f;
            isOpen = false;
        }
    }
}
