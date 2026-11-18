using UnityEngine;

/// <summary>
/// Checkpoint (sztandar ze zniczem). Po dotknieciu zapisuje miejsce odrodzenia,
/// przywraca pelny kolor sztandaru i zapala ogien w zniczu.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer flagRenderer;
    [SerializeField] private GameObject activeEffect;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.7f, 0f);
    [SerializeField] private Color inactiveColor = new Color(0.75f, 0.75f, 0.8f, 1f);
    [SerializeField] private Color activeColor = Color.white;

    private static Checkpoint current;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (current != null && current.gameObject == null)
            current = null;

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

        if (activeEffect != null)
            activeEffect.SetActive(active);
    }
}
