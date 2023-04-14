using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [SerializeField] float _life;
    public float Life() => ReturnNewStats(_life, 2, BuffEnemyType.BuffLife);

    [SerializeField] float _damage;
    public float Damage() => ReturnNewStats(_damage, 1.5f, BuffEnemyType.BuffAttack);


    [SerializeField] float _fireRate;
    public float FireRate() => ReturnNewStats(_fireRate, 1.7f, BuffEnemyType.BuffFireRate);


    [SerializeField] float _defense;
    public float Defense() => ReturnNewStats(_defense, 1.3f, BuffEnemyType.BuffDefense);


    [SerializeField] float _moveSpeed = 2;
    public float MoveSpeed() => ReturnNewStats(_moveSpeed, 1.5f, BuffEnemyType.BuffVelocity);
    public int Score;
    float ReturnNewStats(float stat, float StatMultipier, BuffEnemyType buffType)
    {
        float currentStat = stat;
        if (WaveManager.instance.buffEnemyType == buffType)
        {
            float IncreaseNewStat = stat * StatMultipier;
            return IncreaseNewStat;
        }
        else
        {
            return currentStat;
        }
    }
    public void LifeBehaviour(GameObject explosionParticle, Vector3 posExplosion, LootBag lootBagComp, GameObject enemyDestroy)
    {
        //敵の死
        Instantiate(explosionParticle, posExplosion, Quaternion.identity);
        GameManager.instance.ActualScore += Score;
        EnemySpawner.instance.EnemyAmount--;
        EnemySpawner.instance.enemiesAlive--;
        Debug.Log("poner carta en el mazo");
        lootBagComp.GetComponent<LootBag>().InstantiateLoot();
        Debug.Log("Destruir");
        Destroy(enemyDestroy);
    }
    public void flip(float PosX, float thisPosX, SpriteRenderer sprite)
    {
        if (PosX < thisPosX)
        {
            sprite.flipX = true;
        }
        else
        {
            sprite.flipX = false;
        }
    }
}
