using UnityEngine;

/// <summary>
/// Plynne przesuwanie warstw tla (paralaksa) oraz ich zapetlanie w poziomie wzgledem kamery.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layerTransform;
        [Range(0f, 1f)] public float parallaxFactor = 0.5f;
        public float tileWidth = 22f;
        [HideInInspector] public Vector3 startPos;
    }

    [SerializeField] private Transform targetCamera;
    [SerializeField] private ParallaxLayer[] layers;
    [SerializeField] private float verticalParallaxRatio = 0.25f;

    private Vector3 cameraStartPos;

    private void Start()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;

        if (targetCamera == null) return;

        cameraStartPos = targetCamera.position;

        if (layers == null) return;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerTransform != null)
                layers[i].startPos = layers[i].layerTransform.position;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null || layers == null) return;

        Vector3 camDelta = targetCamera.position - cameraStartPos;

        for (int i = 0; i < layers.Length; i++)
        {
            ParallaxLayer layer = layers[i];
            if (layer.layerTransform == null) continue;

            // parallaxFactor = 1 -> tlo stoi z kamera; 0 -> tlo stoi w swiecie.
            float moveX = camDelta.x * layer.parallaxFactor;
            float moveY = camDelta.y * (1f - (1f - layer.parallaxFactor) * verticalParallaxRatio);

            float newX = layer.startPos.x + moveX;
            if (layer.tileWidth > 0.01f)
            {
                float relativeCamX = targetCamera.position.x - newX;
                if (Mathf.Abs(relativeCamX) >= layer.tileWidth)
                {
                    float shift = Mathf.Round(relativeCamX / layer.tileWidth) * layer.tileWidth;
                    layer.startPos.x += shift;
                    newX += shift;
                }
            }

            layer.layerTransform.position = new Vector3(newX, layer.startPos.y + moveY, layer.startPos.z);
        }
    }
}
