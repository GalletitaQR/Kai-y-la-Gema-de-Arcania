using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo y Seguimiento")]
    public Transform target; // Arrastra aquí tu personaje (Kai)
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 1f, -10f);

    [Header("Límites del Nivel (Bounds)")]
    public bool useBounds = false;
    public float minX = -10f;
    public float maxX = 140f;
    public float minY = -4f;
    public float maxY = 10f;

    [Header("Bloqueo en Sala del Jefe (Tramo 6)")]
    public bool lockInBossRoom = false;
    public float bossRoomTriggerX = 115f;
    public Vector2 bossRoomCameraPos = new Vector2(125f, 1.5f);
    public float bossRoomOrthoSize = 6.5f;

    private bool isLockedInBossRoom = false;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (lockInBossRoom && !isLockedInBossRoom && target.position.x >= bossRoomTriggerX)
        {
            isLockedInBossRoom = true;
        }

        Vector3 desiredPosition;
        if (isLockedInBossRoom)
        {
            desiredPosition = new Vector3(bossRoomCameraPos.x, bossRoomCameraPos.y, offset.z);
            if (cam != null && cam.orthographic && bossRoomOrthoSize > 0f)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, bossRoomOrthoSize, smoothSpeed * Time.deltaTime);
            }
        }
        else
        {
            desiredPosition = target.position + offset;
            if (useBounds)
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
            }
        }

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        smoothedPosition.z = offset.z;
        transform.position = smoothedPosition;
    }

    public float CurrentOrthoSize => cam != null ? cam.orthographicSize : bossRoomOrthoSize;

    public void LockToBossArena(Vector2 arenaCenter, float orthoSize = 6.5f)
    {
        bossRoomCameraPos = arenaCenter;
        if (orthoSize > 0f) bossRoomOrthoSize = orthoSize;
        isLockedInBossRoom = true;
    }

    public void FocusCutscene(Vector2 worldCenter, float orthoSize)
    {
        LockToBossArena(worldCenter, orthoSize);
    }

    public void UnlockBossArena()
    {
        isLockedInBossRoom = false;
    }
}
