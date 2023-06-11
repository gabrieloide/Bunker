using UnityEngine;
using UnityEngine.UI;

public class ManagerTurretLife : MonoBehaviour
{
    Canvas canvas;
    Camera camera;
    public Slider LifeSlider;
    private void Start()
    {
        camera = GetComponent<Camera>();
        canvas = GetComponent<Canvas>();
        canvas.worldCamera = camera;
        LifeSlider.maxValue = GetComponentInParent<TurretCard>().Life;
    }
    
    void Update()
    {
        LifeSlider.value =  GetComponentInParent<TurretCard>().Life;
    }
}
