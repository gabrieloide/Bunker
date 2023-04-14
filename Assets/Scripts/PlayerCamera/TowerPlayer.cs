using UnityEngine;

public class TowerPlayer : MonoBehaviour
{
    public static TowerPlayer instance;
    public Loot[] loot;
    [Range(0, 100)] public float life;
    float dealTime;
    [SerializeField] GameObject hitParticle;
    public float DealTime;
    [SerializeField] LayerMask EnemyLayer;
    [SerializeField] bool canChangeChance;
    [SerializeField] int currentIndex = 9;
    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start() => dealTime = DealTime;
    public void DealDamage(float enemyDamage)
    {
        dealTime -= Time.deltaTime;
        if (dealTime < 0)
        {
            //Recibir da;o a la torre
            life -= enemyDamage;
            Instantiate(hitParticle, transform.position + new Vector3(2, 0), Quaternion.identity, transform);
            dealTime = DealTime;
        }
    }
    private void Update()
    {
        ChangeCardChance();
    }
    public void ChangeCardChance()
    {
        if (Mathf.Floor(life / 10) == currentIndex && canChangeChance)
        {
            foreach (var item in loot)
            {
                item.dropChance += Mathf.FloorToInt(IncreaseChance());
            }
            currentIndex--;
            Debug.Log(IncreaseChance());
            canChangeChance = false;
        }
        else if (Mathf.Floor(life / 10) != currentIndex)
        {

            canChangeChance = true;
        }
    }
    float IncreaseChance()
    {
        int index = 10;
        float currentLife = Mathf.Floor(life / index) * index;
        float chance = 0;

        for (int i = 0; i < index; i++)
        {
            if (Mathf.Floor(life / index) == i)
            {
                chance = (100 - currentLife) / 10;
                return chance;
            }
        }
        return chance;
    }
}