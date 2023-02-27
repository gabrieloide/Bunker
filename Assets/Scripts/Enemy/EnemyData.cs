using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    //Escribir mas stats de enemigos
    public float Life;
    public int score;
    public float Damage;
    public float fireRate;
    public float Defense;
    public float MoveSpeed;
    public void LifeBehaviour(GameObject explosionParticle, Vector3 posExplosion, GameObject lootBagComp, GameObject enemyDestroy)
    {
        Instantiate(explosionParticle, posExplosion, Quaternion.identity);
        GameManager.instance.ActualScore += score;
        EnemySpawner.instance.EnemyAmount--;
        lootBagComp.GetComponent<LootBag>().InstantiateLoot();
        Destroy(enemyDestroy);
    }
    public void flip(float PosX, float thisPosX, SpriteRenderer sprite)
    {
        bool n = PosX < thisPosX ? sprite.flipX = true : sprite.flipX = false;
    }

}
