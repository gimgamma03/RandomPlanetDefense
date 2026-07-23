using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType 
{ 
    Cannon = 0, 
    Laser, Slow, Buff, 
    ChainLightning, Bomb, 
    MultiWayShooting,
    MultiBomb,
    MultiLaser
}

public enum WeaponState 
{ 
    SearchTarget = 0, TryAttackCannon,
    TryAttackLaser, TryAttackChainLightning,
    TryAttackMultiShooting,
    TryAttackMultiBomb
}
public class TowerWeapon : MonoBehaviour
{
    [SerializeField]
    private TowerData towerData;
    [SerializeField]
    private Transform spawnPoint;
    [SerializeField]
    public WeaponType weaponType;

    [Header("Projectile")]
    [SerializeField]
    private GameObject targetProjectilePrefab;

    [Header("BombProjectile")]
    [SerializeField]
    private GameObject bombProjectilePrefab;

    [Header("MultiShoot")]
    [SerializeField]
    private GameObject projectilePrefab;

    [Header("MultiBomb")]
    [SerializeField]
    private GameObject bombPrefab;

    [Header("Laser")]
    [SerializeField]
    private LineRenderer lineRenderer;

    [Header("MultiLaserPlus")]
    [SerializeField]
    private LineRenderer lineRenderer2;
    [SerializeField]
    private LineRenderer lineRenderer3;

    private Slow slow;
    private GameObjectPoolManager poolManager;

    public WeaponState weaponState = WeaponState.SearchTarget;
    private Transform attackTarget = null;
    private Vector3 attackTargetVector;
    private SpriteRenderer spriteRenderer;

    private TowerSpawner towerSpawner;
    private EnemySpawner enemySpawner;

    public TowerGrade towerGrade;
    public Sprite towerSprite => towerData.sprite;
    public int level = 1; // ?? ? ? ? ??
    public int upGradeGold;
    public int useGoldToUpGrade = 0;

    [HideInInspector]
    public float damage;
    [HideInInspector]
    public float range;
    [HideInInspector]
    public float rate;
    bool doubleShot;

    //? ?
    [HideInInspector]
    public float slowValue;

    // Start is called before the first frame update
    void Start()
    {
        this.damage = towerData.weapon.damage;
        this.range = towerData.weapon.range;
        this.rate = towerData.weapon.rate;
        this.doubleShot = towerData.weapon.doubleShot;

        if (weaponType == WeaponType.Slow)
        {
            this.slowValue = towerData.weapon.slowValue;
            slow.SetUp(slowValue, range);
        }

        upGradeGold = (int)towerGrade * Constants.upGradeGoldMulti;
    }

    public void SetUp(TowerSpawner towerSpawner, EnemySpawner enemySpawner)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        this.towerSpawner = towerSpawner;
        this.enemySpawner = enemySpawner;
        poolManager = GameObjectPoolManager.EnsureExists();

        if (!(weaponType == WeaponType.Slow || weaponType == WeaponType.Buff))
        {
            ChangeState(WeaponState.SearchTarget);
        }
        else if (weaponType == WeaponType.Slow)
        {
            slow = GetComponentInChildren<Slow>();
        }
    }

    private GameObject SpawnPooled(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (poolManager == null)
        {
            poolManager = GameObjectPoolManager.EnsureExists();
        }

        if (poolManager != null && prefab != null)
        {
            return poolManager.Spawn(prefab, position, rotation);
        }

        return Instantiate(prefab, position, rotation);
    }
    // Update is called once per frame

    public bool UPGrade()
    {
        useGoldToUpGrade += upGradeGold;
        upGradeGold += (int)towerGrade;
        damage += towerData.weaponUpGradeValue.damage;
        range += towerData.weaponUpGradeValue.range;
        rate += towerData.weaponUpGradeValue.rate;


        if (weaponType == WeaponType.Slow)
        {
            slowValue += towerData.weaponUpGradeValue.slowValue;
            slow.SetUp(slowValue, range);
        }

        level++;

        return true;
    }
    public void ChangeState(WeaponState newState)
    {
        //  ?  
        StopCoroutine(weaponState.ToString());
        //  
        weaponState = newState;
        // ?  
        StartCoroutine(weaponState.ToString());
    }
    public IEnumerator SearchTarget()
    {
        while (true)
        {
            //     ?  () 
            attackTarget = FindClosestAttackTarget();

            if (attackTarget != null)
            {
                switch(weaponType)
                {
                    case WeaponType.Cannon:
                    case WeaponType.Bomb:
                        ChangeState(WeaponState.TryAttackCannon);
                        break;

                    case WeaponType.ChainLightning:
                        ChangeState(WeaponState.TryAttackChainLightning);
                        break;

                    case WeaponType.Laser:
                        ChangeState(WeaponState.TryAttackLaser);
                        break;

                    case WeaponType.MultiWayShooting:
                        ChangeState(WeaponState.TryAttackMultiShooting);
                        break;

                    case WeaponType.MultiBomb:
                        ChangeState(WeaponState.TryAttackMultiBomb);
                        break;
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    private Transform FindClosestAttackTarget()
    {
        //   ?  ?    ? ? 
        float closestDistSqr = Mathf.Infinity;
        // EnemySpawner EnemyList ?  ? ?   ?
        for (int i = 0; i < enemySpawner.enemyList.Count; ++i)
        {
            float distance = Vector3.Distance(enemySpawner.enemyList[i].transform.position, transform.position);
            //  ?   ?  ?,  ?   
            if (distance <= range && distance <= closestDistSqr)
            {
                closestDistSqr = distance;
                attackTarget = enemySpawner.enemyList[i].transform;
            }
        }

        return attackTarget;
    }

    private bool IsPossibleToAttackTarget()
    {
        // target ? ? (? ?  , Goal  ?  )
        if (attackTarget == null)
        {
            return false;
        }

        // target   ? ? ? (  ? ?  )
        float distance = Vector3.Distance(attackTarget.position, transform.position);
        if (distance > range)
        {
            attackTarget = null;
            return false;
        }

        return true;
    }

    private IEnumerator TryAttackMultiBomb()
    {
        while (true)
        {
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }

            SpawnMultiBomb();

            yield return new WaitForSeconds(rate);

        }
    }

    private IEnumerator TryAttackMultiShooting()
    {
        while (true)
        {
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget);
                break;
            }

            attackTargetVector = attackTarget.transform.position;

            SpawnMultiProjectile(attackTargetVector);

            //?   ?
            if (doubleShot)
            {
                yield return new WaitForSeconds(0.2f);
                SpawnMultiProjectile(attackTargetVector);
            }

            yield return new WaitForSeconds(rate);
        }
    }
    private IEnumerator TryAttackChainLightning()
    {
        if (IsPossibleToAttackTarget() == false)
        {
            ChangeState(WeaponState.SearchTarget);
            yield return null;
        }
        else
        {
            ChainLightning lightning = GetComponent<ChainLightning>();
            lightning.SetUp(attackTarget.gameObject, damage);

            //try-catch use?
            lightning.ChainLightningStart();
 
            yield return new WaitForSeconds(rate);
            ChangeState(WeaponState.SearchTarget);
        }
    }
    private IEnumerator TryAttackCannon()
    {
        while (true)
        {
            if (IsPossibleToAttackTarget() == false)
            {
                ChangeState(WeaponState.SearchTarget); 
                break;
            }

            if (weaponType == WeaponType.Cannon)
            {
                SpawnTargetProjectile();
            }
            else if (weaponType == WeaponType.Bomb)
            {
                SpawnBombProjectile();
            }

            yield return new WaitForSeconds(rate);
        }
    }

    private IEnumerator TryAttackLaser()
    {
        // ,   ? ??
        EnableLaser();

        while (true)
        {
            // target ?  ?
            if (IsPossibleToAttackTarget() == false)
            {
                // ,   ? ??
                DisableLaser();

                //enable and Research cooltime
                yield return new WaitForSeconds(rate);

                ChangeState(WeaponState.SearchTarget);
                break;
            }

            //  
            SpawnLaser();

            yield return new WaitForEndOfFrame();
        }
    }
    private void SpawnMultiBomb()
    {
        Vector2 bombDirectoin = (attackTarget.transform.position - transform.position).normalized;
        Vector2 towerPositon = transform.position;
        float bombRange = 5.0f;
        int bombCount = 5;
        Vector2 bombVector = bombDirectoin * bombRange;

        Vector2[] bombPosition = new Vector2[bombCount];

        for(int i = 0; i < bombCount; i++)
        {
            bombPosition[i] = towerPositon + bombVector * ((i + 1) * (1.0f / bombCount));
            GameObject bombs = SpawnPooled(bombPrefab, bombPosition[i], Quaternion.identity);
            bombs.GetComponent<Bomb>().SetUp(damage);
        }

    }
    private void SpawnMultiProjectile(Vector3 attackTargetPosition)
    {
        Vector3 []targetMove = new Vector3[3] ;
        targetMove[0] = (attackTargetPosition - transform.position);
        targetMove[1] = Quaternion.AngleAxis(45f, Vector3.forward) * targetMove[0];
        targetMove[2] = Quaternion.AngleAxis(-45f, Vector3.forward) * targetMove[0];

        GameObject [] projectileClones = new GameObject[3];
        float damage = this.damage; //+add damage

        for (int i = 0; i < projectileClones.Length; i++)
        {
            projectileClones[i] = SpawnPooled(projectilePrefab, spawnPoint.position, Quaternion.identity);
            projectileClones[i].GetComponent<Projectile>().Setup(targetMove[i], damage);
        }
    }
    private void SpawnBombProjectile()
    {
        GameObject clone = SpawnPooled(bombProjectilePrefab, spawnPoint.position, Quaternion.identity);
        //  ? ?(attackTarget)  
        // ? =  ? ? +   ? ?
        float damage = this.damage; // +Adddamage
        clone.GetComponent<BombProjectile>().Setup(attackTarget, damage);
    }
    private void SpawnTargetProjectile()
    {
        GameObject clone = SpawnPooled(targetProjectilePrefab, spawnPoint.position, Quaternion.identity);
        //  ? ?(attackTarget)  
        // ? =  ? ? +   ? ?
        float damage = this.damage; // +Adddamage
        clone.GetComponent<TargetProjectile>().Setup(attackTarget, damage);
    }

    private void EnableLaser()
    {
        lineRenderer.gameObject.SetActive(true);
        //hitEffect.gameObject.SetActive(true);
    }

    private void DisableLaser()
    {
        lineRenderer.gameObject.SetActive(false);
        //hitEffect.gameObject.SetActive(false);
    }

    private void SpawnLaser()
    {
        Vector3 direction = attackTarget.position - spawnPoint.position;
        //RaycastHit2D[] hit = Physics2D.RaycastAll(spawnPoint.position, direction, towerData.weapon[level].range, targetLayer);
        RaycastHit2D[] hit = Physics2D.RaycastAll(spawnPoint.position, direction, this.range);

        //          attackTarget  ? 
        for (int i = 0; i < hit.Length; ++i)
        {
            if (hit[i].transform == attackTarget)
            {
                //  
                lineRenderer.SetPosition(0, spawnPoint.position);
                //  ?
                lineRenderer.SetPosition(1, new Vector3(hit[i].point.x, hit[i].point.y, 0) + Vector3.back);
                //  ? ? 
                //hitEffect.position = hit[i].point;
                //    (1? damage? )
                // ? =  ? ? +   ? ?
                float damage = this.damage; // + AddedDamage;
                attackTarget.GetComponent<EnemyHp>().TakeDamage(damage * Time.deltaTime);
            }
        }
    }


}
