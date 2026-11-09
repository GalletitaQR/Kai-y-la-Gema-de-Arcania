using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Referencias")]
    public Transform cameraTransform;

    [Header("Configuración")]
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f; // 0 = se mueve con el mundo, 1 = sigue a la cámara
    [Range(0f, 1f)]
    public float parallaxFactorY = 0f;

    private Vector3 lastCameraPosition;
    private bool initialized = false;

    void Start()
    {
        InitCamera();
    }

    private void InitCamera()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (cameraTransform != null)
        {
            lastCameraPosition = cameraTransform.position;
            initialized = true;
        }
    }

    void LateUpdate()
    {
        if (!initialized)
        {
            InitCamera();
            if (!initialized) return;
        }

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        transform.position += new Vector3(
            deltaMovement.x * parallaxFactor,
            deltaMovement.y * parallaxFactorY,
            0f
        );
        lastCameraPosition = cameraTransform.position;
    }
}
