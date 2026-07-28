using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D rigidbody2d;
    private Vector3 target;
    private float moveSpeed = 4.5f;
    private float damage;
    private ProjectileVfx vfx;
    private SpriteRenderer spriteRenderer;

    // ChargePierce가 같은 Straight 풀에서 스프라이트/스케일을 바꾸므로 재사용 전 복구
    private Sprite defaultSprite;
    private Color defaultColor = Color.white;
    private Vector3 defaultScale = Vector3.one;
    private bool defaultsCaptured;

    private bool pierce;
    private bool despawnWhenOffScreen;
    private float maxTravelDistance;
    private float spinSpeed;
    private Vector3 spawnPosition;
    private readonly HashSet<int> piercedEnemyIds = new HashSet<int>();

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        CaptureVisualDefaults();

        vfx = GetComponent<ProjectileVfx>();
        if (vfx == null)
        {
            vfx = gameObject.AddComponent<ProjectileVfx>();
        }
    }

    private const float NormalLifetime = 2f;
    private const float NormalMaxTravelDistance = 9f;

    public void Setup(Vector3 target, float damage)
    {
        RestoreVisualDefaults();
        pierce = false;
        despawnWhenOffScreen = false;
        piercedEnemyIds.Clear();
        spinSpeed = 0f;
        spawnPosition = transform.position;
        maxTravelDistance = NormalMaxTravelDistance;
        ApplySetup(target, damage, NormalLifetime, enableTrail: true);
    }

    /// <summary>직진 관통 — 적마다 1회 피해, 화면 밖으로 나가면 소멸.</summary>
    public void SetupPierce(Vector3 direction, float damage, float spinSpeed = 360f)
    {
        // 비주얼은 ChargePierceBehavior.ApplyMeteoVisual이 SetupPierce 전에 적용
        pierce = true;
        despawnWhenOffScreen = true;
        piercedEnemyIds.Clear();
        this.spinSpeed = spinSpeed;
        maxTravelDistance = 0f;
        spawnPosition = transform.position;

        Vector3 normalized = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.right;

        ApplySetup(normalized, damage, 25f, enableTrail: false);
    }

    private void CaptureVisualDefaults()
    {
        if (defaultsCaptured)
        {
            return;
        }

        if (spriteRenderer != null)
        {
            defaultSprite = spriteRenderer.sprite;
            defaultColor = spriteRenderer.color;
        }

        defaultScale = transform.localScale.sqrMagnitude > 0.0001f
            ? transform.localScale
            : Vector3.one;
        defaultsCaptured = true;
    }

    private void RestoreVisualDefaults()
    {
        CaptureVisualDefaults();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = defaultSprite;
            spriteRenderer.color = defaultColor;
        }

        transform.localScale = defaultScale;
        transform.rotation = Quaternion.identity;

        if (vfx != null)
        {
            vfx.ResetToPrefabVisual(defaultScale);
        }
    }

    private void ApplySetup(Vector3 direction, float damage, float lifetime, bool enableTrail = true)
    {
        CancelInvoke();
        target = direction;
        this.damage = damage;

        if (rigidbody2d != null)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            rigidbody2d.angularVelocity = 0f;
        }

        ProjectileFacing.FaceDirection(transform, direction);
        AddForceToTarget(direction);

        if (vfx != null)
        {
            vfx.SetTrailEnabled(enableTrail);
            vfx.BeginFlight();
        }

        Invoke(nameof(ReleaseMiss), lifetime);
    }

    private void Update()
    {
        if (pierce && spinSpeed != 0f)
        {
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        if (despawnWhenOffScreen && IsOffCamera())
        {
            ReleaseMiss();
            return;
        }

        if (maxTravelDistance > 0f &&
            Vector3.Distance(spawnPosition, transform.position) >= maxTravelDistance)
        {
            ReleaseMiss();
        }
    }

    private bool IsOffCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        Vector3 viewport = cam.WorldToViewportPoint(transform.position);
        const float margin = 0.12f;
        return viewport.z < 0f
            || viewport.x < -margin
            || viewport.x > 1f + margin
            || viewport.y < -margin
            || viewport.y > 1f + margin;
    }

    public void AddForceToTarget(Vector3 target)
    {
        if (rigidbody2d == null)
        {
            return;
        }

        Vector2 dir = ((Vector2)target).sqrMagnitude > 0.0001f
            ? ((Vector2)target).normalized
            : Vector2.right;
        rigidbody2d.linearVelocity = dir * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.CompareTag("Enemy"))
        {
            return;
        }

        if (!collision.gameObject.activeInHierarchy)
        {
            return;
        }

        EnemyHp enemyHp = collision.GetComponent<EnemyHp>();
        if (enemyHp != null)
        {
            if (pierce)
            {
                int enemyId = collision.GetInstanceID();
                if (!piercedEnemyIds.Add(enemyId))
                {
                    return;
                }

                enemyHp.TakeDamage(damage);
                return;
            }

            enemyHp.TakeDamage(damage);
        }

        Release(hit: true);
    }

    private void ReleaseMiss()
    {
        Release(hit: false);
    }

    private void Release(bool hit)
    {
        CancelInvoke();

        if (vfx != null)
        {
            if (hit)
            {
                vfx.NotifyHit(transform.position);
            }
            else
            {
                vfx.NotifyMiss();
            }
        }

        if (rigidbody2d != null)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            rigidbody2d.angularVelocity = 0f;
        }

        // 풀 반환 전 메테오 비주얼 잔여 제거 (MultiShot과 Straight 풀 공유)
        RestoreVisualDefaults();
        pierce = false;
        despawnWhenOffScreen = false;
        spinSpeed = 0f;
        maxTravelDistance = 0f;
        piercedEnemyIds.Clear();

        ProjectileLifecycle.ReturnToPool(gameObject);
    }
}
