using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타겟을 추적해 날아가다 적에 닿으면 범위 폭발.
/// AoE TakeDamage는 프레임 분산(코루틴) — 폭발 판정은 즉시.
/// </summary>
public class BombProjectile : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 11f;

    [SerializeField]
    private float bombRadius = 1.6f;

    [SerializeField]
    private float hitDistance = 0.35f;

    [SerializeField]
    private float explodeVfxLife = 0.45f;

    [SerializeField]
    private float maxFlightTime = 4f;

    [SerializeField]
    [Min(1)]
    private int aoeDamagePerFrame = AoEDamageBatch.DefaultPerFrame;

    [SerializeField]
    private ParticleSystem bombParticle;

    private readonly List<EnemyHp> aoeTargets = new List<EnemyHp>(32);

    private Transform target;
    private Vector3 lastTargetPosition;
    private float damage;
    private float flightStartTime;
    private bool isExploding;
    private bool hasDamaged;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rigidbody2d;
    private Coroutine damageRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        if (rigidbody2d != null)
        {
            rigidbody2d.bodyType = RigidbodyType2D.Kinematic;
            rigidbody2d.simulated = true;
        }
    }

    public void Setup(Transform attackTarget, float damage)
    {
        StopAllCoroutines();
        CancelInvoke();
        damageRoutine = null;

        this.damage = damage;
        target = attackTarget;
        lastTargetPosition = attackTarget != null ? attackTarget.position : transform.position;
        isExploding = false;
        hasDamaged = false;
        flightStartTime = Time.time;
        aoeTargets.Clear();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (bombParticle != null)
        {
            bombParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        ProjectileFacing.FacePoint(transform, lastTargetPosition);
    }

    private void Update()
    {
        if (isExploding)
        {
            return;
        }

        if (Time.time - flightStartTime >= maxFlightTime)
        {
            Explode();
            return;
        }

        Vector3 aim = GetAimPosition();
        Vector3 toTarget = aim - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= hitDistance)
        {
            Explode();
            return;
        }

        Vector3 direction = toTarget / distance;
        ProjectileFacing.FaceDirection(transform, direction);
        transform.position += direction * (moveSpeed * Time.deltaTime);
    }

    private Vector3 GetAimPosition()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            EnemyHp hp = target.GetComponent<EnemyHp>();
            if (hp == null || !hp.IsDead)
            {
                lastTargetPosition = target.position;
                return lastTargetPosition;
            }
        }

        target = null;
        return lastTargetPosition;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isExploding || collision == null || !collision.CompareTag("Enemy"))
        {
            return;
        }

        if (!collision.gameObject.activeInHierarchy)
        {
            return;
        }

        Explode();
    }

    private void Explode()
    {
        if (isExploding)
        {
            return;
        }

        isExploding = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (bombParticle != null)
        {
            bombParticle.Play();
        }

        BeginAoEDamage();
        StartCoroutine(ReleaseAfterVfx());
    }

    private void BeginAoEDamage()
    {
        if (hasDamaged)
        {
            return;
        }

        hasDamaged = true;
        AoEDamageBatch.CollectEnemyTargets(transform.position, bombRadius, aoeTargets);
        damageRoutine = StartCoroutine(
            AoEDamageBatch.ApplyOverFrames(aoeTargets, damage, aoeDamagePerFrame));
    }

    private IEnumerator ReleaseAfterVfx()
    {
        yield return new WaitForSeconds(explodeVfxLife);
        if (damageRoutine != null)
        {
            yield return damageRoutine;
            damageRoutine = null;
        }

        Release();
    }

    private void Release()
    {
        StopAllCoroutines();
        damageRoutine = null;
        isExploding = false;
        hasDamaged = false;
        target = null;
        aoeTargets.Clear();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        ProjectileLifecycle.ReturnToPool(gameObject);
    }
}
