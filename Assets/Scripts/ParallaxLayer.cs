using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Referencias")]
    public Transform cameraTransform;

    [Header("Configuración")]
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f; // 0 = fijo a la cámara, 1 = se mueve igual que la cámara

    private Vector3 lastCameraPosition;

    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(deltaMovement.x * parallaxFactor, deltaMovement.y * parallaxFactor, 0f);
        lastCameraPosition = cameraTransform.position;
    }
}