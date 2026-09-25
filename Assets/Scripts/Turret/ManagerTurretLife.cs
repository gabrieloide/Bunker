using UnityEngine;
using UnityEngine.UI;

public class ManagerTurretLife : MonoBehaviour
{
    Canvas canvas;
    Camera camera;
    public Slider LifeSlider;
    private TurretCard turretCard;
    private float lastLife = -1f;

    private void Start()
    {
        camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        canvas = GetComponent<Canvas>();
        if (canvas != null)
            canvas.worldCamera = camera;

        turretCard = GetComponentInParent<TurretCard>();
        if (turretCard != null && LifeSlider != null)
        {
            LifeSlider.maxValue = turretCard.Life;
            LifeSlider.value = turretCard.Life;
            lastLife = turretCard.Life;
        }
    }

    void Update()
    {
        if (turretCard != null && LifeSlider != null)
        {
            // A life buff raises the max as well
            if (turretCard.MaxLife > 0f && !Mathf.Approximately(LifeSlider.maxValue, turretCard.MaxLife))
                LifeSlider.maxValue = turretCard.MaxLife;
            float currentLife = turretCard.Life;
            if (!Mathf.Approximately(currentLife, lastLife))
            {
                lastLife = currentLife;
                LifeSlider.value = currentLife;
            }
        }
    }
}
