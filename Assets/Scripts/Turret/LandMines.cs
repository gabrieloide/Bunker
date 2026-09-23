using UnityEngine;

public class LandMines : MonoBehaviour, ICardConfigurable
{
    [SerializeField] AK.Wwise.Event ExplotionLandMine;
    [SerializeField] GameObject ExplosionParticle;

    // LEGACY: moved to LandMineCardDefinition, dropped after migration
    [HideInInspector][SerializeField] float damage;
    [HideInInspector][SerializeField] float penArmor;
    public float LegacyDamage => damage;
    public float LegacyPenArmor => penArmor;

    LandMineCardDefinition definition;

    public void Configure(CardDefinition card) => definition = card as LandMineCardDefinition;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            //Explosion de mina de tierra
            ExplotionLandMine.Post(gameObject);
            Instantiate(ExplosionParticle, transform.position, Quaternion.identity);
            if (definition != null)
                collision.GetComponent<Enemy>().Damage(definition.damage, definition.bulletPen, gameObject);
            else
                Debug.LogWarning($"[LandMines] {name} was placed without a LandMineCardDefinition; no damage dealt.", this);
            Destroy(gameObject);
        }
    }
}
