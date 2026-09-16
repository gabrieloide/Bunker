using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirAttackManager : TurretStats
{
    [SerializeField] AK.Wwise.Event PlaneSound;
    [SerializeField] AK.Wwise.Event PlaneShoot;

    [SerializeField] GameObject BulletAA;
    [SerializeField] GameObject PlaneGO;
    [SerializeField] GameObject ShadowPlaneGO;

    [SerializeField] float offsetPlaneY;
    [SerializeField] float airPlaneSpeed;
    [SerializeField] private float distance;
    private float diametroGO;
    Camera camera;
    [SerializeField] float bulletSpeed;
    [SerializeField] private float delayBtwBullets;
    [SerializeField] private float threshold;
    private Vector3 MousePosition;
    private void Start()
    {
        LaunchPlane();
    }
    void LaunchPlane()
    {
        PlaneSound.Post(gameObject);
        camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        Vector3 position = camera != null ? GetPointLeftOfCamera(camera, distance, diametroGO) : Vector3.zero;
        MousePosition = camera != null ? camera.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
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
    void PlaneBehaviour(Vector3 position)
    {
        GameObject newPlane = Instantiate(PlaneGO,
            new Vector3(position.x, MousePosition.y + offsetPlaneY, transform.position.z),
            Quaternion.identity);

        newPlane.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(airPlaneSpeed, 0);

        Instantiate(ShadowPlaneGO, new Vector3(newPlane.transform.position.x, MousePosition.y, 0),
            Quaternion.identity, newPlane.transform);
        StartCoroutine(checkDistance(newPlane, threshold));
    }
    IEnumerator checkDistance(GameObject airPlanePos, float threshold)
    {
        while (Vector3.Distance(airPlanePos.transform.position, MousePosition) >= threshold)
            yield return new WaitForSeconds(0.1f);

        StartCoroutine(bulletMovement(airPlanePos));
    }
    IEnumerator bulletMovement(GameObject airPlanePos)
    {
        for (int i = 0; i < 5; i++)
        {
            Shoot(airPlanePos);
            yield return new WaitForSeconds(delayBtwBullets);
        }
    }
    void Shoot(GameObject airPlanePos)
    {
        PlaneShoot.Post(gameObject);
        if (SimulationBridge.Instance == null)
            return;

        Vector3 origin = airPlanePos.transform.position;
        Vector3 fireDirection = new Vector3(1f, -1f, 0f).normalized;
        Vector3 aimPoint = origin + fireDirection * 15f;
        SimulationBridge.Instance.SpawnTowerProjectile(origin, aimPoint, damage, bulletPen);
    }
}
