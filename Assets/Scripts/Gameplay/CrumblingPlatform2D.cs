using System.Collections;
using UnityEngine;

/// <summary>
/// Kruszaca sie platforma 2D: po wskoczeniu gracza od gory drzy przez chwile,
/// zmienia kolor ostrzegawczy, zapada sie i znika, a po krotkim czasie odnawia sie.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CrumblingPlatform2D : MonoBehaviour
{
    [SerializeField] private float shakeDuration = 0.65f;
    [SerializeField] private float shakeMagnitude = 0.06f;
    [SerializeField] private float fallFadeDuration = 0.20f;
    [SerializeField] private float respawnDelay = 2.5f;
    [SerializeField] private Color idleColor = new Color(1f, 0.82f, 0.62f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.38f, 0.22f, 1f);

    private Collider2D mainCollider;
    private Collider2D[] allColliders;
    private SpriteRenderer[] renderers;
    private Vector3 startLocalPos;
    private bool triggered;

    private void Awake()
    {
        mainCollider = GetComponent<Collider2D>();
        allColliders = GetComponentsInChildren<Collider2D>(true);
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        startLocalPos = transform.localPosition;

        SetRenderersColor(idleColor);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTrigger(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryTrigger(collision);
    }

    private void TryTrigger(Collision2D collision)
    {
        if (triggered) return;

        BallController player = collision.collider.GetComponentInParent<BallController>();
        if (player == null) return;

        if (mainCollider != null && collision.collider.bounds.min.y < mainCollider.bounds.center.y - 0.2f)
            return;

        triggered = true;
        StartCoroutine(CrumbleRoutine());
    }

    private IEnumerator CrumbleRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shakeDuration);

            Vector2 jitter = Random.insideUnitCircle * shakeMagnitude;
            transform.localPosition = startLocalPos + new Vector3(jitter.x, jitter.y, 0f);
            SetRenderersColor(Color.Lerp(idleColor, warningColor, t));
            yield return null;
        }

        SetCollidersEnabled(false);

        Vector3 dropEndPos = startLocalPos + Vector3.down * 0.35f;
        elapsed = 0f;
        while (elapsed < fallFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallFadeDuration);

            transform.localPosition = Vector3.Lerp(startLocalPos, dropEndPos, t);
            Color faded = warningColor;
            faded.a = 1f - t;
            SetRenderersColor(faded);
            yield return null;
        }

        SetRenderersColor(new Color(warningColor.r, warningColor.g, warningColor.b, 0f));

        yield return new WaitForSeconds(respawnDelay);

        transform.localPosition = startLocalPos;
        SetRenderersColor(idleColor);
        SetCollidersEnabled(true);
        triggered = false;
    }

    private void SetCollidersEnabled(bool enabledState)
    {
        for (int i = 0; i < allColliders.Length; i++)
        {
            if (allColliders[i] != null)
                allColliders[i].enabled = enabledState;
        }
    }

    private void SetRenderersColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = color;
        }
    }
}
