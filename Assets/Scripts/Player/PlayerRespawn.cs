using UnityEngine;

/// <summary>
/// Pamieta, gdzie gracz ma sie odrodzic (start poziomu albo ostatni checkpoint).
/// </summary>
public class PlayerRespawn : MonoBehaviour
{
    private Vector3 respawnPoint;
    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        respawnPoint = transform.position;
    }

    public void SetCheckpoint(Vector3 point)
    {
        respawnPoint = point;
    }

    public void MoveToRespawnPoint()
    {
        transform.position = respawnPoint;

        if (body == null) return;
        body.position = respawnPoint;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }
}
