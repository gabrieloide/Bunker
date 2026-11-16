using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirAttackManager : TurretStats, ICardConfigurable
{
    [SerializeField] Sfx PlaneSound = Sfx.PlaneFlyby;
    [SerializeField] Sfx PlaneShoot = Sfx.PlaneShot;

    [SerializeField] GameObject BulletAA;
    [SerializeField] GameObject PlaneGO;
    [SerializeField] GameObject ShadowPlaneGO;

    [SerializeField] float offsetPlaneY;
    [SerializeField] float airPlaneSpeed;
    [SerializeField] private float distance;
    private float diametroGO;
    Camera mainCamera;
    [SerializeField] private float threshold;
    private Vector3 target;
    AirStrikeCardDefinition definition;
    bool isShooting;

    public void Configure(CardDefinition card) => definition = card as AirStrikeCardDefinition;

    private void Start()
    {
        LaunchPlane();
    }
    void LaunchPlane()
    {
        target = transform.position;
        SoundBank.Play(PlaneSound);
        mainCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        Vector3 position = mainCamera != null
            ? GetPointLeftOfCamera(mainCamera, distance, diametroGO)
            : new Vector3(target.x - 15f, target.y, 0f);
        PlaneBehaviour(position);
    }
    public static Vector3 GetPointLeftOfCamera(Camera camera, float distance, float goDiameter)
    {
        var ray = camera.ScreenPointToRay(new Vector3(0, camera.pixelHeight / 2f, 0));
        var borderPoint = ray.GetPoint(distance);
        var leftPlane = GeometryUtility.CalculateFrustumPlanes(camera)[0];
        var frustumLeft = leftPlane.normal;
        var halfDiameter = goDiameter / 2f;
        return borderPoint + frustumLeft * halfDiameter;
    }
    static float GetRightEdgeX(Camera camera, float distance, float fallbackX)
    {
        if (camera == null)
            return fallbackX;
        var ray = camera.ScreenPointToRay(new Vector3(camera.pixelWidth, camera.pixelHeight / 2f, 0));
        return ray.GetPoint(distance).x + 2f;
    }
    void PlaneBehaviour(Vector3 position)
    {
        GameObject newPlane = Instantiate(PlaneGO,
            new Vector3(position.x, target.y + offsetPlaneY, transform.position.z),
            Quaternion.identity);

        if (newPlane.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = new Vector2(airPlaneSpeed, 0f);

        if (ShadowPlaneGO != null)
        {
            Instantiate(ShadowPlaneGO, new Vector3(newPlane.transform.position.x, target.y, 0f),
                Quaternion.identity, newPlane.transform);
        }

        StartCoroutine(checkDistance(newPlane, threshold));
        StartCoroutine(DestroyPlaneOffscreen(newPlane));
    }
    IEnumerator checkDistance(GameObject airPlanePos, float threshold)
    {
        while (airPlanePos != null && Mathf.Abs(airPlanePos.transform.position.x - target.x) > threshold)
            yield return null;

        if (airPlanePos == null)
            yield break;

        yield return StartCoroutine(bulletMovement(airPlanePos));
    }
    IEnumerator bulletMovement(GameObject airPlanePos)
    {
        if (definition == null)
        {
            Debug.LogWarning($"[AirAttackManager] {name} was spawned without an AirStrikeCardDefinition; no shots fired.", this);
            yield break;
        }
        isShooting = true;
        try
        {
            for (int i = 0; i < definition.shotCount; i++)
            {
                if (airPlanePos == null)
                    yield break;
                Shoot(airPlanePos);
                yield return new WaitForSeconds(definition.shotInterval);
            }
        }
        finally
        {
            isShooting = false;
        }
    }
    IEnumerator DestroyPlaneOffscreen(GameObject airPlanePos)
    {
        while (airPlanePos != null)
        {
            float rightEdgeX = GetRightEdgeX(mainCamera, distance, target.x + 20f);
            if (airPlanePos.transform.position.x > rightEdgeX)
            {
                Destroy(airPlanePos);
                break;
            }
            yield return null;
        }
        while (isShooting)
            yield return null;
        Destroy(gameObject);
    }
    void Shoot(GameObject airPlanePos)
    {
        if (airPlanePos == null)
            return;

        SoundBank.Play(PlaneShoot);
        if (SimulationBridge.Instance == null)
            return;

        Vector3 origin = airPlanePos.transform.position;
        Vector3 aimPoint = new Vector3(origin.x, target.y, 0f);
        SimulationBridge.Instance.SpawnTowerProjectile(origin, aimPoint, definition.damage, definition.bulletPen);
    }
}
