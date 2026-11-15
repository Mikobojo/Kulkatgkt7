using UnityEngine;

public class CrystalPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        BallController player =
            other.GetComponentInParent<BallController>();

        if (player == null) return;

        collected = true;
        player.AddPoints(value);
        Destroy(gameObject);
    }
}