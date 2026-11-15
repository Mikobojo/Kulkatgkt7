using UnityEngine;

/// <summary>
/// Checkpoint (flaga). Po dotknieciu przez gracza zapisuje miejsce odrodzenia
/// i zmienia kolor flagi. Aktywny jest zawsze tylko ostatni checkpoint.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.7f, 0f);
    [SerializeField] private Color inactiveColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color activeColor = new Color(0.55f, 1f, 0.55f, 1f);

    private static Checkpoint current;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (flagRenderer == null)
            flagRenderer = GetComponentInChildren<SpriteRenderer>();

        SetVisualActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;
        if (current == this) return;

        respawn.SetCheckpoint(transform.position + spawnOffset);

        if (current != null)
            current.SetVisualActive(false);

        current = this;
        SetVisualActive(true);
    }

    private void SetVisualActive(bool active)
    {
        if (flagRenderer != null)
            flagRenderer.color = active ? activeColor : inactiveColor;
    }
}
