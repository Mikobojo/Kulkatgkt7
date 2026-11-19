using UnityEngine;

/// <summary>
/// Wirujaca, poruszajaca sie kula z kolcami: porusza sie ruchem wahadlowym
/// i zabija gracza przy kontakcie.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MovingHazardBall : MonoBehaviour
{
    [SerializeField] private Vector2 moveOffset = new Vector2(0f, 2.4f);
    [SerializeField] private float cycleSpeed = 2.2f;
    [SerializeField] private float rotationSpeed = 240f;
    [SerializeField] private float phaseOffset = 0f;
    [SerializeField] private string deathReason = "Trafiła Cię kolczasta kula!";

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * cycleSpeed + phaseOffset) + 1f) * 0.5f;
        transform.position = Vector3.Lerp(startPosition, startPosition + (Vector3)moveOffset, t);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayer(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryKillPlayer(collision.collider);
    }

    private void TryKillPlayer(Collider2D other)
    {
        PlayerDeath playerDeath = other.GetComponentInParent<PlayerDeath>();
        if (playerDeath == null || playerDeath.IsDead) return;

        playerDeath.Die(deathReason);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? startPosition : transform.position;
        Vector3 dest = origin + (Vector3)moveOffset;

        Gizmos.color = new Color(1f, 0.35f, 0.15f, 1f);
        Gizmos.DrawLine(origin, dest);
        Gizmos.DrawWireSphere(origin, 0.2f);
        Gizmos.DrawWireSphere(dest, 0.2f);
    }
}
