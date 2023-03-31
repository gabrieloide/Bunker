using UnityEngine;
using UnityEngine.UI;

public class ManagerTurretLife : MonoBehaviour
{
    Canvas canvas;
    Camera camera;
    public Slider LifeSlider;
    public TowerD towerD;
    private void Start()
    {
        camera = GetComponent<Camera>();
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = camera;
    }
    float currentLife()
    {
        var tD = GetComponentInParent<TowerD>();
        return tD.timeToDestroy;

    }
    
    void Update()
    {
        LifeSlider.value = currentLife();
    }
}
