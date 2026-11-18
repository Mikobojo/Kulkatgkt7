using System.Collections;
using UnityEngine;

/// <summary>
/// Przeciwnik, ktorego mozna zabic skaczac na niego z gory.
/// Dotkniecie z boku zabija gracza (chyba ze gracz jest chwilowo niesmiertelny po odrodzeniu).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StompableEnemy : MonoBehaviour
{
    [SerializeField] private float bounceVelocity = 8f;
    [SerializeField] private int pointsReward = 0;
    [SerializeField] private float deathAnimationTime = 0.25f;

    private Collider2D enemyCollider;
    private bool dead;

    private void Awake()
    {
        enemyCollider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (dead) return;

        BallController player = collision.collider.GetComponentInParent<BallController>();
        if (player == null) return;

        PlayerDeath playerDeath = player.GetComponent<PlayerDeath>();
        if (playerDeath != null && playerDeath.IsDead) return;

        if (HitFromAbove(collision))
        {
            Stomp(player);
        }
        else if (playerDeath != null && !playerDeath.IsInvulnerable)
        {
            playerDeath.Die("Pokonał Cię przeciwnik!");
        }
    }

    // Skok na przeciwnika = jakikolwiek punkt styku w gornej polowie jego collidera.
    // Dotkniecie z boku (np. toczac sie po ziemi) trafia w dolna polowe.
    private bool HitFromAbove(Collision2D collision)
    {
        float middleY = enemyCollider.bounds.center.y;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).point.y >= middleY)
                return true;
        }

        return false;
    }

    private void Stomp(BallController player)
    {
        dead = true;

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        playerBody.linearVelocity = new Vector2(playerBody.linearVelocity.x, bounceVelocity);

        if (pointsReward > 0)
            player.AddPoints(pointsReward);

        PatrolEnemy patrol = GetComponent<PatrolEnemy>();
        if (patrol != null) patrol.enabled = false;
        enemyCollider.enabled = false;

        StartCoroutine(DeathAnimation());
    }

    private IEnumerator DeathAnimation()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < deathAnimationTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / deathAnimationTime);
            transform.localScale = new Vector3(startScale.x * (1f + t * 0.3f), startScale.y * (1f - t), startScale.z);
            yield return null;
        }

        Destroy(gameObject);
    }
}
