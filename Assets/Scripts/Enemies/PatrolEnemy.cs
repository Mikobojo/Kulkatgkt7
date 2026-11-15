using UnityEngine;

/// <summary>
/// Przeciwnik chodzacy w lewo i prawo wokol swojej pozycji startowej.
/// Sam ruch - zabijanie gracza i zabijanie przeciwnika jest w StompableEnemy.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float patrolLeft = 3f;
    [SerializeField] private float patrolRight = 3f;
    [SerializeField] private bool spriteFacesRight;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D body;
    private float startX;
    private int direction = 1;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        startX = transform.position.x;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        float x = body.position.x;

        if (direction > 0 && x >= startX + patrolRight)
        {
            direction = -1;
            UpdateFacing();
        }
        else if (direction < 0 && x <= startX - patrolLeft)
        {
            direction = 1;
            UpdateFacing();
        }

        body.MovePosition(body.position + Vector2.right * (direction * moveSpeed * Time.fixedDeltaTime));
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = spriteFacesRight ? direction < 0 : direction > 0;
    }

    private void OnDrawGizmosSelected()
    {
        float center = Application.isPlaying ? startX : transform.position.x;
        Vector3 left = new Vector3(center - patrolLeft, transform.position.y, 0f);
        Vector3 right = new Vector3(center + patrolRight, transform.position.y, 0f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.1f);
        Gizmos.DrawWireSphere(right, 0.1f);
    }
}
