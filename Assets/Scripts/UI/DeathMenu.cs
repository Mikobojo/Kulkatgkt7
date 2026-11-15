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
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private PlayerDeath player;
    [SerializeField] private LevelRestart levelRestart;

    private void Awake()
    {
        if (player == null) player = FindFirstObjectByType<PlayerDeath>();
        if (levelRestart == null) levelRestart = FindFirstObjectByType<LevelRestart>();

        panel.SetActive(false);
    }

    public void Show(string reason, int score)
    {
        reasonText.text = reason;
        scoreText.text = "Twoj wynik: " + score;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    // Podpiete pod przyciski w panelu.
    public void RespawnAtCheckpoint()
    {
        player.Revive();
    }

    public void RestartLevel()
    {
        levelRestart.RestartLevel();
    }
}
