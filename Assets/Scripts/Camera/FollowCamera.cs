using UnityEngine;

/// <summary>
/// Plynne podazanie kamery za graczem z wyprzedzeniem i dolnym limitem wysokosci.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    public GameObject target;

    [SerializeField] private Vector2 offset = new Vector2(1.2f, 0.9f);
    [SerializeField] private float smoothTime = 0.14f;
    [SerializeField] private float minY = -1.5f;

    private Vector3 velocity;

    private void Start()
    {
        if (target == null) return;
        Vector3 initial = GetTargetPosition();
        transform.position = initial;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = GetTargetPosition();
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    private Vector3 GetTargetPosition()
    {
        Vector3 pos = target.transform.position;
        return new Vector3(
            pos.x + offset.x,
            Mathf.Max(minY, pos.y + offset.y),
            transform.position.z
        );
    }
}
