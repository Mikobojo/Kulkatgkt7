using TMPro;
using UnityEngine;

/// <summary>
/// Wyswietla aktualna liczbe zebranych krysztalow w HUD.
/// </summary>
public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private BallController player;
    [SerializeField] private ExitGate exitGate;
    [SerializeField] private TMP_Text scoreText;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<BallController>();

        if (exitGate == null)
            exitGate = FindFirstObjectByType<ExitGate>();
    }

    private void Update()
    {
        if (player == null || scoreText == null) return;

        string suffix = exitGate != null ? " / " + exitGate.GetRequiredPoints() : "";
        scoreText.text = "Kryształy: " + player.GetPoints() + suffix;
    }
}
