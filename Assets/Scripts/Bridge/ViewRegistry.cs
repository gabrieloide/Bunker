using System.Collections.Generic;
using Bunker.Simulation;
using Unity.Entities;
using UnityEngine;

public class ViewRegistry
{
    public struct EntityView
    {
        public GameObject GameObject;
        public Transform Transform;
        public ViewKind Kind;
        public int PrefabId;
        public Enemy Enemy;
        public TurretCard TurretCard;
        public Bullet Bullet;
        public AllyView Ally;
    }

    private readonly Dictionary<Entity, EntityView> views = new Dictionary<Entity, EntityView>();
    private readonly GameObject[] enemyPrefabs;
    private readonly Queue<GameObject>[] enemyPools;

    public int ViewCount => views.Count;
    public Dictionary<Entity, EntityView>.Enumerator GetEnumerator() => views.GetEnumerator();

    public ViewRegistry(GameObject[] prefabs)
    {
        enemyPrefabs = prefabs ?? new GameObject[0];
        enemyPools = new Queue<GameObject>[enemyPrefabs.Length];
        for (int i = 0; i < enemyPools.Length; i++)
            enemyPools[i] = new Queue<GameObject>();
    }

    public bool TryGetView(Entity entity, out EntityView view)
    {
        return views.TryGetValue(entity, out view);
    }

    public GameObject GetEnemyView(int prefabId, Vector3 position)
    {
        if (enemyPools != null && prefabId >= 0 && prefabId < enemyPools.Length)
        {
            var poolQueue = enemyPools[prefabId];
            while (poolQueue.Count > 0)
            {
                var instance = poolQueue.Dequeue();
                if (instance != null)
                {
                    instance.transform.position = position;
                    instance.SetActive(true);
                    if (instance.TryGetComponent<Enemy>(out var enemy))
                        enemy.ResetForPool();
                    return instance;
                }
            }
        }

        if (prefabId >= 0 && prefabId < enemyPrefabs.Length && enemyPrefabs[prefabId] != null)
        {
            var view = Object.Instantiate(enemyPrefabs[prefabId], position, Quaternion.identity);
            MakeKinematic(view);
            return view;
        }

        Debug.LogWarning($"ViewRegistry: invalid enemy prefabId {prefabId}");
        return null;
    }

    public void RegisterView(Entity entity, GameObject view, ViewKind kind, int prefabId = 0)
    {
        if (view == null) return;

        var ev = new EntityView
        {
            GameObject = view,
            Transform = view.transform,
            Kind = kind,
            PrefabId = prefabId,
            Enemy = view.GetComponent<Enemy>(),
            TurretCard = view.GetComponent<TurretCard>(),
            Bullet = view.GetComponent<Bullet>(),
            Ally = view.GetComponent<AllyView>()
        };
        views[entity] = ev;
    }

    public void RemoveView(Entity entity)
    {
        if (views.TryGetValue(entity, out var view))
        {
            if (view.GameObject != null)
            {
                if (view.Kind == ViewKind.TowerProjectile || view.Kind == ViewKind.EnemyProjectile)
                {
                    view.GameObject.SetActive(false);
                }
                else if (view.Kind == ViewKind.Enemy)
                {
                    ReturnEnemyView(view.PrefabId, view.GameObject, view.Enemy);
                }
                else
                {
                    Object.Destroy(view.GameObject);
                }
            }
            views.Remove(entity);
        }
    }

    private void ReturnEnemyView(int prefabId, GameObject view, Enemy enemy)
    {
        if (view == null) return;

        if (enemy != null)
            enemy.Entity = Entity.Null;

        view.SetActive(false);

        if (enemyPools != null && prefabId >= 0 && prefabId < enemyPools.Length)
        {
            enemyPools[prefabId].Enqueue(view);
        }
        else
        {
            Object.Destroy(view);
        }
    }

    public static void MakeKinematic(GameObject view)
    {
        if (view.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Clear()
    {
        if (enemyPools != null)
        {
            for (int i = 0; i < enemyPools.Length; i++)
            {
                if (enemyPools[i] != null)
                    enemyPools[i].Clear();
            }
        }
        views.Clear();
    }
}
