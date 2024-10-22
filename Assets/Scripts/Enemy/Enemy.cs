﻿using System.Collections;
using UnityEngine;
public class Enemy : NPCBehaviour, IDamageable
{
    public EnemyData Data;
    
    EnemyMovement enemyMovement;
    [SerializeField] GameObject hitParticle, explosionParticle;
    LootBag LootBagCom;
    [HideInInspector] public float Life;

    private void Awake()
    {
        damage = Data.Damage();
        Hitable = LayerMask.GetMask("Turret");
    }
    protected override void BehaviourFinalPath()
    {
        GetComponent<Animator>().enabled = false;
        StartCoroutine(TowerPlayer.instance.DealDamage(Data.Damage()));
    }
    private void Start()
    {
        LootBagCom = FindObjectOfType<LootBag>();
        enemyMovement = FindObjectOfType<EnemyMovement>();
        StartCoroutine(MoveAlongPath(enemyMovement.points, false, gameObject, Data.MoveSpeed(), transform));
        Life = Data.Life();
    }
    private void Update()
    {
        if (Life < 0)
        {
            Data.LifeBehaviour(explosionParticle, transform.position, LootBagCom, gameObject);
            destroy.Post(gameObject);
        }
    }
    public void Damage(float damage, float bulletPen, GameObject deactivateBullet)
    {
        //Rebre mal
        float realDamage = Mathf.Max(0, damage - (bulletPen - Data.Defense()));

        hitParticle.SetActive(true);
        Life -= realDamage;
        destroy.Post(gameObject);
        deactivateBullet.SetActive(false);
    }
    public IEnumerator DamageTextMovement(float offsetDamageTextY, float damageTextTime)
    {
        GameObject dt = ObjectPooling.instance.TextDamage();
        dt.SetActive(true);
        dt.transform.position = transform.position;
        LeanTween.move(dt, transform.position + new Vector3(default, offsetDamageTextY, 0), damageTextTime).setEaseOutQuad();
        yield return new WaitForSeconds(0.5f);
        dt.SetActive(false);
    }
}