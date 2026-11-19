using System.Collections;
using UnityEngine;

/// <summary>
/// Sprezysta wyrzutnia, ktora po najechaniu lub skoku od gory
/// wystrzeliwuje gracza (oraz inne dynamiczne obiekty) wysoko w gore.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BouncePad : MonoBehaviour
{
    [SerializeField] private float bounceForce = 11.2f;
    [SerializeField] private float cooldown = 0.15f;
    [SerializeField] private Transform visualTransform;
    [SerializeField] private float popHeight = 0.30f;
    [SerializeField] private AudioSource bounceAudio;

    private Collider2D padCollider;
    private Vector3 restLocalPos;
    private float lastBounceTime = -10f;
    private Coroutine popCoroutine;

    private void Awake()
    {
        padCollider = GetComponent<Collider2D>();
        if (visualTransform == null)
            visualTransform = transform;

        restLocalPos = visualTransform.localPosition;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBounce(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryBounce(collision);
    }

    private void TryBounce(Collision2D collision)
    {
        if (Time.time < lastBounceTime + cooldown) return;

        Rigidbody2D otherRb = collision.rigidbody;
        if (otherRb == null || otherRb.bodyType != RigidbodyType2D.Dynamic) return;

        if (collision.collider.bounds.min.y < padCollider.bounds.center.y - 0.15f) return;

        otherRb.linearVelocity = new Vector2(otherRb.linearVelocity.x, bounceForce);
        lastBounceTime = Time.time;

        if (bounceAudio != null && bounceAudio.clip != null)
            bounceAudio.Play();

        if (popCoroutine != null)
            StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(AnimatePop());
    }

    private IEnumerator AnimatePop()
    {
        Vector3 peakPos = restLocalPos + Vector3.up * popHeight;
        const float riseDuration = 0.07f;
        const float returnDuration = 0.22f;

        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            visualTransform.localPosition = Vector3.Lerp(restLocalPos, peakPos, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            visualTransform.localPosition = Vector3.Lerp(peakPos, restLocalPos, t);
            yield return null;
        }

        visualTransform.localPosition = restLocalPos;
        popCoroutine = null;
    }
}
