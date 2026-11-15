using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private BallController player;
    [SerializeField] private TMP_Text scoreText;

    private void Update()
    {
        if (player == null || scoreText == null) return;
        scoreText.text = "Punkty: " + player.GetPoints();
    }
}