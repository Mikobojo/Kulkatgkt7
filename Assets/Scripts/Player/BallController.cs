using UnityEngine;

/// <summary>
/// Sterowanie kulka gracza: ruch w poziomie, niezawodny skok (coyote time + bufor) oraz zliczanie punktow.
/// </summary>
public class BallController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.5f;
    public float maxSpeed = 6.5f;

    [Header("Jump")]
    public float jumpForce = 7.8f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.18f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Score")]
    [SerializeField] private int points;

    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private float moveInput;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (IsGrounded())
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    private void FixedUpdate()
    {
        rb.AddForce(new Vector2(moveInput * moveSpeed, 0f), ForceMode2D.Force);

        if (Mathf.Abs(rb.linearVelocity.x) > maxSpeed)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxSpeed, rb.linearVelocity.y);
        }
    }

    private bool IsGrounded()
    {
        if (circleCollider == null)
            return false;

        Bounds b = circleCollider.bounds;
        float castRadius = b.extents.x * 0.85f;
        float castDistance = (b.extents.y - castRadius) + groundCheckDistance;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(b.center, castRadius, Vector2.down, castDistance, groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || hit.collider == circleCollider || hit.collider.isTrigger)
                continue;

            if (hit.normal.y > 0.35f)
                return true;
        }

        return false;
    }

    public void AddPoints(int amount)
    {
        points += amount;
    }

    public int GetPoints()
    {
        return points;
    }

    private void OnDrawGizmosSelected()
    {
        CircleCollider2D col = circleCollider != null ? circleCollider : GetComponent<CircleCollider2D>();
        if (col == null) return;

        Gizmos.color = Color.yellow;
        Vector3 bottom = new Vector3(col.bounds.center.x, col.bounds.min.y, 0f);
        Gizmos.DrawWireSphere(bottom, 0.06f);
        Gizmos.DrawLine(bottom, bottom + Vector3.down * groundCheckDistance);
    }
}
