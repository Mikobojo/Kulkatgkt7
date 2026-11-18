using UnityEngine;

/// <summary>
/// Przeszkoda z kolcami: kontakt zabija gracza i pokazuje menu smierci.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HazardSpike : MonoBehaviour
{
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

        playerDeath.Die("Wpadłeś na kolce!");
    }
}
