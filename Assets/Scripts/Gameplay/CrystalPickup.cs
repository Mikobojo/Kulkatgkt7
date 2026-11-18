using UnityEngine;

/// <summary>
/// Krysztal do zebrania z plynnym unoszeniem sie w powietrzu i pulsowaniem poswiaty.
/// </summary>
public class CrystalPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 2.8f;

    private Vector3 startPosition;
    private float phaseOffset;
    private Transform glowTransform;
    private Vector3 glowBaseScale;
    private bool collected;

    private void Start()
    {
        startPosition = transform.position;
        phaseOffset = startPosition.x * 0.7f;

        glowTransform = transform.Find("Glow");
        if (glowTransform != null)
            glowBaseScale = glowTransform.localScale;
    }

    private void Update()
    {
        if (collected) return;

        float wave = Mathf.Sin(Time.time * bobSpeed + phaseOffset);
        transform.position = startPosition + Vector3.up * (wave * bobAmplitude);

        if (glowTransform != null)
        {
            float scalePulse = 1f + 0.12f * Mathf.Sin(Time.time * bobSpeed * 1.4f + phaseOffset);
            glowTransform.localScale = glowBaseScale * scalePulse;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        BallController player = other.GetComponentInParent<BallController>();
        if (player == null) return;

        collected = true;
        player.AddPoints(value);
        Destroy(gameObject);
    }
}
