using UnityEngine;

public class LandMines : MonoBehaviour
{
    [SerializeField] AK.Wwise.Event ExplotionLandMine;
    public TowersData LandMineData;
    [SerializeField] GameObject ExplosionParticle;
    [SerializeField] float damage;
    [SerializeField] float penArmor;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            //Explosion de mina de tierra
            ExplotionLandMine.Post(gameObject);
            Instantiate(ExplosionParticle, transform.position, Quaternion.identity);
            collision.GetComponent<Enemy>().Damage(damage, penArmor, gameObject);
            Destroy(gameObject);
        }
    }
}
