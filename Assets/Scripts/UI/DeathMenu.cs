using TMPro;
using UnityEngine;

/// <summary>
/// Menu smierci: komunikat, wynik gracza oraz przyciski
/// "Wroc do checkpointu" i "Zagraj od nowa".
/// Ten skrypt ma byc na zawsze aktywnym obiekcie - przelaczany jest tylko "panel".
/// </summary>
public class DeathMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private PlayerDeath player;
    [SerializeField] private LevelRestart levelRestart;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerDeath>();
        if (levelRestart == null) levelRestart = FindFirstObjectByType<LevelRestart>();

        if (panel != null)
            panel.SetActive(false);
    }

    public void Show(string reason, int score)
    {
        if (hudRoot != null)
            hudRoot.SetActive(false);

        if (reasonText != null)
            reasonText.text = reason;

        if (scoreText != null)
            scoreText.text = "Zdobyte kryształy: " + score;

        if (panel != null)
            panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);

        if (hudRoot != null)
            hudRoot.SetActive(true);
    }

    // Podpiete pod przyciski w panelu.
    public void RespawnAtCheckpoint()
    {
        if (player != null)
            player.Revive();
    }

    public void RestartLevel()
    {
        if (levelRestart != null)
            levelRestart.RestartLevel();
    }
}
