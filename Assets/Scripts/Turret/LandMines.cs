using UnityEngine;

public class LandMines : MonoBehaviour, ICardConfigurable
{
    [SerializeField] Sfx ExplotionLandMine = Sfx.MineExplosion;
    [SerializeField] GameObject ExplosionParticle;

    LandMineCardDefinition definition;

    public void Configure(CardDefinition card) => definition = card as LandMineCardDefinition;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            SoundBank.Play(ExplotionLandMine);
            if (ExplosionParticle != null)
            {
                var pos = new Vector3(transform.position.x, transform.position.y, 0f);
                var fx = Instantiate(ExplosionParticle, pos, Quaternion.identity);
                OneShotEffect.EnsureFrontSorting(fx);
            }

            if (definition != null)
                collision.GetComponent<Enemy>().Damage(definition.damage, definition.bulletPen, gameObject);
            else
                Debug.LogWarning($"[LandMines] {name} was placed without a LandMineCardDefinition; no damage dealt.", this);
            Destroy(gameObject);
        }
    }
}
