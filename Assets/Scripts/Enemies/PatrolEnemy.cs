using UnityEngine;

/// <summary>
/// Przeciwnik chodzacy w lewo i prawo wokol swojej pozycji startowej,
/// zawracajacy takze przed scianami, skrzyniami oraz krawedziami platform.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float patrolLeft = 3f;
    [SerializeField] private float patrolRight = 3f;
    [SerializeField] private bool spriteFacesRight;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float edgeCheckAhead = 0.45f;
    [SerializeField] private float wallCheckDistance = 0.25f;

    private Rigidbody2D body;
    private Collider2D enemyCollider;
    private float startX;
    private int direction = 1;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
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
            TurnAround(-1);
        }
        else if (direction < 0 && x <= startX - patrolLeft)
        {
            TurnAround(1);
        }
        else if (HitWallAhead() || ReachedPlatformEdge())
        {
            TurnAround(-direction);
        }

        body.MovePosition(body.position + Vector2.right * (direction * moveSpeed * Time.fixedDeltaTime));
    }

    private void TurnAround(int newDirection)
    {
        direction = newDirection;
        UpdateFacing();
    }

    private bool HitWallAhead()
    {
        if (enemyCollider == null) return false;

        Bounds b = enemyCollider.bounds;
        float originX = direction > 0 ? b.max.x + 0.02f : b.min.x - 0.02f;
        Vector2 origin = new Vector2(originX, b.center.y);
        Vector2 dir = direction > 0 ? Vector2.right : Vector2.left;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, wallCheckDistance);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null || col == enemyCollider || col.isTrigger) continue;
            if (col.GetComponentInParent<BallController>() != null) continue;
            return true;
        }
        return false;
    }

    private bool ReachedPlatformEdge()
    {
        if (enemyCollider == null) return false;

        Bounds b = enemyCollider.bounds;
        // Sprawdz najpierw czy w ogole stoi na jakims podlozu.
        if (!HasGroundAt(new Vector2(b.center.x, b.min.y + 0.05f)))
            return false;

        float aheadX = direction > 0 ? b.max.x + edgeCheckAhead : b.min.x - edgeCheckAhead;
        return !HasGroundAt(new Vector2(aheadX, b.min.y + 0.05f));
    }

    private bool HasGroundAt(Vector2 origin)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 0.4f);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null || col == enemyCollider || col.isTrigger) continue;
            if (col.GetComponentInParent<BallController>() != null) continue;
            return true;
        }
        return false;
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
