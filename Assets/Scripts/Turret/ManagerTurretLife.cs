using UnityEngine;
using UnityEngine.UI;

public class ManagerTurretLife : MonoBehaviour
{
    Canvas canvas;
    Camera camera;
    public Slider LifeSlider;
    public TurretCard towerD;
    public float life;
    private void Start()
    {
        camera = GetComponent<Camera>();
        canvas = GetComponent<Canvas>();
        towerD = GetComponentInParent<TurretCard>();
        canvas.worldCamera = camera;
        life = towerD.timeToDestroy;
    }
    
    void Update()
    {
        life -= Time.deltaTime;
        LifeSlider.value = life - Time.deltaTime;
    }
}
