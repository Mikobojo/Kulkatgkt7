using UnityEngine;

/// <summary>
/// Prosta animacja z kolejnych sprite'ow (bez Animatora).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 10f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        int index = (int)(Time.time * framesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[index];
    }
}
