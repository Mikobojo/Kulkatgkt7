using UnityEngine;

/// <summary>
/// Smierc gracza: upadek w przepasc albo zabicie przez przeciwnika lub kolce.
/// Pokazuje menu smierci i pozwala odrodzic gracza w ostatnim checkpoincie.
/// </summary>
[RequireComponent(typeof(BallController))]
[RequireComponent(typeof(PlayerRespawn))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private float fallDeathY = -9f;
    [SerializeField] private float respawnInvulnerabilityDuration = 0.6f;
    [SerializeField] private DeathMenu deathMenu;

    private BallController ball;
    private PlayerRespawn respawn;
    private Rigidbody2D body;
    private float invulnerableUntil;

    public bool IsDead { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    private void Awake()
    {
        ball = GetComponent<BallController>();
        respawn = GetComponent<PlayerRespawn>();
        body = GetComponent<Rigidbody2D>();

        if (deathMenu == null)
            deathMenu = FindFirstObjectByType<DeathMenu>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (!IsDead && transform.position.y < fallDeathY)
            Die("Spadłeś w przepaść!");
    }

    public void Die(string reason)
    {
        if (IsDead || IsInvulnerable) return;
        // Po wygranej (bramka wylacza ruch gracza) nie da sie juz umrzec.
        if (!ball.enabled) return;

        IsDead = true;
        ball.enabled = false;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;

        if (deathMenu != null)
            deathMenu.Show(reason, ball.GetPoints());
    }

    public void Revive()
    {
        if (!IsDead) return;

        IsDead = false;
        invulnerableUntil = Time.time + respawnInvulnerabilityDuration;
        respawn.MoveToRespawnPoint();
        body.simulated = true;
        ball.enabled = true;

        if (deathMenu != null)
            deathMenu.Hide();
    }
}
