using TMPro;
using UnityEngine;

/// <summary>
/// Oltarz wyjscia: po zebraniu wymaganej liczby krysztalow runa swieci na szmaragdowo-cyjanowo
/// i pozwala ukonczyc poziom.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ExitGate : MonoBehaviour
{
    [SerializeField] private BallController player;
    [SerializeField] private TMP_Text goalText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private SpriteRenderer glowRenderer;
    [SerializeField] private AudioSource unlockAudio;
    [SerializeField] private int requiredPoints = 16;

    private SpriteRenderer gateRenderer;
    private bool won;

    private void Awake()
    {
        gateRenderer = GetComponent<SpriteRenderer>();

        if (player == null)
            player = FindFirstObjectByType<BallController>();
    }

    private void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void Update()
    {
        if (won || player == null) return;

        int currentPoints = player.GetPoints();
        bool ready = currentPoints >= requiredPoints;
        float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 4f);

        if (glowRenderer != null)
        {
            gateRenderer.color = Color.white;
            Color baseColor = ready
                ? new Color(0.15f, 1f, 0.85f, 1f)
                : new Color(1f, 0.3f, 0.2f, 1f);
            glowRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, pulse);
        }
        else
        {
            gateRenderer.color = ready ? Color.green : Color.red;
        }

        if (goalText != null)
        {
            goalText.text = ready
                ? "Portal otwarty! Dotknij ołtarza!"
                : "Cel: Zbierz " + requiredPoints + " kryształów (" + currentPoints + "/" + requiredPoints + ")";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (won || player == null) return;

        BallController visitor = other.GetComponentInParent<BallController>();
        if (visitor != player) return;
        if (player.GetPoints() < requiredPoints) return;

        won = true;
        if (goalText != null)
            goalText.text = "Poziom ukończony!";

        if (unlockAudio != null && unlockAudio.clip != null)
            unlockAudio.Play();

        if (hudRoot != null)
            hudRoot.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(true);

        player.enabled = false;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
    }

    public int GetRequiredPoints()
    {
        return requiredPoints;
    }
}
