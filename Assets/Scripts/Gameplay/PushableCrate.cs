using UnityEngine;

/// <summary>
/// Skrzynia, ktora gracz moze przepychac (np. zeby wejsc na wysoka sciane).
/// Jesli spadnie w przepasc, wraca na swoje miejsce startowe.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PushableCrate : MonoBehaviour
{
    [SerializeField] private float mass = 0.4f;
    [SerializeField] private float fallResetY = -9f;

    private Rigidbody2D body;
    private Vector3 startPosition;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.mass = mass;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        startPosition = transform.position;
    }

    private void Update()
    {
        if (transform.position.y < fallResetY)
            ResetToStart();
    }

    public void ResetToStart()
    {
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.position = startPosition;
        transform.position = startPosition;
    }
}
