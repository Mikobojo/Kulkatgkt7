using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kinematyczna ruchoma platforma 2D (pozioma, pionowa lub ukosna),
/// ktora bezpiecznie przewozi toczaca sie kulke gracza oraz dynamiczne obiekty.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    [SerializeField] private Vector2 moveOffset = new Vector2(6f, 0f);
    [SerializeField] private float speed = 2.4f;
    [SerializeField] private float pauseDuration = 0.5f;

    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private bool movingToEnd = true;
    private float pauseTimer;

    private readonly HashSet<Rigidbody2D> passengers = new HashSet<Rigidbody2D>();
    private readonly List<Rigidbody2D> stalePassengers = new List<Rigidbody2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        startPosition = rb.position;
        endPosition = startPosition + moveOffset;
        movingToEnd = true;
    }

    private void FixedUpdate()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 current = rb.position;
        Vector2 target = movingToEnd ? endPosition : startPosition;
        Vector2 next = Vector2.MoveTowards(current, target, speed * Time.fixedDeltaTime);
        Vector2 delta = next - current;

        rb.MovePosition(next);

        if (delta.sqrMagnitude > 0f)
            CarryPassengers(delta);

        if ((next - target).sqrMagnitude < 0.0001f)
        {
            movingToEnd = !movingToEnd;
            pauseTimer = pauseDuration;
        }
    }

    private void CarryPassengers(Vector2 delta)
    {
        if (passengers.Count == 0) return;

        stalePassengers.Clear();
        foreach (Rigidbody2D passenger in passengers)
        {
            if (passenger == null || !passenger.simulated)
            {
                stalePassengers.Add(passenger);
                continue;
            }

            Vector2 carry = new Vector2(
                delta.x,
                (delta.y < 0f && passenger.linearVelocity.y <= 0.5f) ? delta.y : 0f
            );
            passenger.position += carry;
        }

        for (int i = 0; i < stalePassengers.Count; i++)
            passengers.Remove(stalePassengers[i]);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryAddPassenger(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryAddPassenger(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
            passengers.Remove(collision.rigidbody);
    }

    private void TryAddPassenger(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;
        if (otherRb == null || otherRb.bodyType != RigidbodyType2D.Dynamic) return;

        if (platformCollider == null || collision.collider.bounds.min.y >= platformCollider.bounds.center.y - 0.15f)
            passengers.Add(otherRb);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Vector2 dest = origin + moveOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, dest);
        Gizmos.DrawWireSphere(origin, 0.15f);
        Gizmos.DrawWireSphere(dest, 0.15f);
    }
}
