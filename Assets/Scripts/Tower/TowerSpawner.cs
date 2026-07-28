using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TowerSpawner : MonoBehaviour
{
    [SerializeField]
    private Tilemap WallMap;

    [Tooltip("있으면 이걸로 CSV 적용. 없으면 Resources/TowerBalance.csv 자동")]
    [SerializeField]
    private MonoBehaviour balanceSourceBehaviour;

    [Tooltip("CSV로 SO 덮어쓰기. 끄면 Resources CSV가 있어도 SO 수치만 사용")]
    [SerializeField]
    private bool applyBalanceCsv = true;

    [Header("Addressables Pilot")]
    [Tooltip("ON이면 Base를 주소로 로드. OFF면 예전처럼 Library 직접 참조")]
    [SerializeField]
    private bool useAddressablesForBases;

    [Tooltip("비우면 같은 오브젝트에 TowerBasePrefabLoader를 자동 추가")]
    [SerializeField]
    private TowerBasePrefabLoader basePrefabLoader;

    private TowerCatalog catalog;
    private TowerBaseLibrary baseLibrary;
    private List<GameObject> towerList;
    private IPlayerService playerService;
    private ITowerBalanceSource balanceSource;
    private IPlaySessionStatsService sessionStats;

    void Awake()
    {
        towerList = new List<GameObject>();
        EnsureCatalog();
        EnsureBaseLoader();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (GetComponent<TowerDebugSpawner>() == null)
        {
            gameObject.AddComponent<TowerDebugSpawner>();
        }
#endif
    }

    private void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
        ServiceLocator.TryGet(out sessionStats);
        EnsureCatalog();
        EnsureBaseLoader();
        TryApplyBalance();
    }

    private void EnsurePlayerService()
    {
        if (playerService == null)
        {
            playerService = ServiceLocator.Get<IPlayerService>();
        }
    }

    private void EnsureCatalog()
    {
        if (catalog == null)
        {
            catalog = TowerCatalog.LoadFromResources();
        }

        if (baseLibrary == null)
        {
            baseLibrary = TowerBaseLibrary.Load();
            if (baseLibrary == null)
            {
                Debug.LogWarning("[TowerSpawner] TowerBaseLibrary missing — weaponType Base를 해석할 수 없음.");
            }
        }
    }

    private void EnsureBaseLoader()
    {
        if (basePrefabLoader == null)
        {
            basePrefabLoader = GetComponent<TowerBasePrefabLoader>();
        }

        if (basePrefabLoader == null)
        {
            basePrefabLoader = gameObject.AddComponent<TowerBasePrefabLoader>();
        }

        basePrefabLoader.SetUseAddressables(useAddressablesForBases);
        basePrefabLoader.EnsureLibrary();
    }

    private void TryApplyBalance()
    {
        if (!applyBalanceCsv || catalog == null)
        {
            return;
        }

        balanceSource = balanceSourceBehaviour as ITowerBalanceSource;
        if (balanceSource != null)
        {
            balanceSource.ApplyToCatalog(catalog);
            return;
        }

        TextAsset csv = Resources.Load<TextAsset>("TowerBalance");
        if (csv != null)
        {
            TowerBalanceCsv.Apply(catalog, csv.text);
        }
    }

    public void SpawnTower(Vector2 towerSpawnPosition)
    {
        SpawnTower(towerSpawnPosition, (TowerGrade)Constants.ShopSpawnGrade);
    }

    public void SpawnTower(Vector2 towerLocation, TowerGrade grade)
    {
        StartCoroutine(SpawnTowerRoutine(towerLocation, grade, null));
    }

    /// <summary>디버그·연출용 지정 스폰. 골드 차감 없이 선택한 TowerData를 사용한다.</summary>
    public void SpawnTower(Vector2 towerLocation, TowerData towerData)
    {
        if (towerData == null)
        {
            Debug.LogError("[TowerSpawner] 지정 스폰 TowerData가 null입니다.");
            return;
        }

        StartCoroutine(SpawnTowerRoutine(towerLocation, towerData.grade, towerData));
    }

    public bool SpawnTowerById(Vector2 towerLocation, string towerId)
    {
        EnsureCatalog();
        if (catalog == null || !catalog.TryGet(towerId, out TowerData towerData))
        {
            Debug.LogError($"[TowerSpawner] TowerData id를 찾을 수 없습니다: {towerId}");
            return false;
        }

        SpawnTower(towerLocation, towerData);
        return true;
    }

    private IEnumerator SpawnTowerRoutine(
        Vector2 towerLocation,
        TowerGrade grade,
        TowerData specifiedData)
    {
        if (towerList == null)
        {
            towerList = new List<GameObject>();
        }

        if (MapDirector.Instance == null || MapDirector.Instance.aStarGrid == null)
        {
            Debug.LogError("[TowerSpawner] MapDirector not ready.");
            yield break;
        }

        AStarNode wallNode = MapDirector.Instance.aStarGrid.GetNodeFromWorld(towerLocation);
        if (wallNode != null && wallNode.isBuildTower)
        {
            Debug.Log("[TowerSpawner] 이미 타워가 있는 셀입니다.");
            yield break;
        }

        if (WallMap == null)
        {
            Debug.LogError("[TowerSpawner] WallMap not assigned.");
            yield break;
        }

        Vector3Int tilePosition = MapDirector.Instance.WallMap.WorldToCell(towerLocation);
        Vector3 tileCenterPosition = MapDirector.Instance.WallMap.GetCellCenterWorld(tilePosition);
        tileCenterPosition -= WallMap.cellGap / 2f;

        EnsureCatalog();
        EnsureBaseLoader();
        if (catalog == null)
        {
            yield break;
        }

        TowerData picked = specifiedData != null ? specifiedData : catalog.PickRandom(grade);
        if (picked == null)
        {
            Debug.LogError($"[TowerSpawner] grade {(int)grade} 풀이 비었음.");
            yield break;
        }

        GameObject prefab = null;
        yield return basePrefabLoader.LoadBasePrefab(picked.weaponType, loaded => prefab = loaded);

        if (prefab == null)
        {
            prefab = catalog.ResolvePrefab(picked, baseLibrary);
        }

        if (prefab == null)
        {
            Debug.LogError($"[TowerSpawner] '{picked.Id}' 프리팹 없음. Library/Addressables 확인.");
            yield break;
        }

        GameObject spawnTower =
            Instantiate(prefab, tileCenterPosition, Quaternion.identity, transform);

        TowerWeapon spawnTowerWeapon = spawnTower.GetComponent<TowerWeapon>();
        Tower spawnTowerScript = spawnTower.GetComponent<Tower>();
        if (spawnTowerWeapon == null || spawnTowerScript == null)
        {
            Debug.LogError($"[TowerSpawner] Prefab '{spawnTower.name}' missing TowerWeapon/Tower.");
            Destroy(spawnTower);
            yield break;
        }

        if (!ServiceLocator.IsRegistered<IEnemyRegistry>())
        {
            Debug.LogError("[TowerSpawner] IEnemyRegistry not registered (EnemySpawner missing?).");
            Destroy(spawnTower);
            yield break;
        }

        spawnTowerWeapon.BindDefinition(picked);

        towerList.Add(spawnTower);
        spawnTowerWeapon.SetUp(this);
        spawnTowerScript.SetUp(this, MapDirector.Instance.aStarGrid.GetNodeFromWorld(tileCenterPosition));
        sessionStats?.RecordTowerSpawned(picked.weaponType);
    }

    public void CombineTower(GameObject tower)
    {
        if (!TowerCombineRules.TryCollectMaterials(
                tower,
                towerList,
                out List<GameObject> materials,
                out TowerGrade nextGrade,
                out WeaponType mergedType))
        {
            return;
        }

        Vector3 spawnPos = tower.transform.position;

        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].GetComponent<Tower>().DestoryThisTower();
        }

        EnsureCatalog();
        if (catalog == null || !catalog.HasAny(nextGrade))
        {
            Debug.LogError($"[TowerSpawner] No towers for grade {(int)nextGrade}.");
            return;
        }

        sessionStats?.RecordTowerMerged(mergedType);
        SpawnTower(spawnPos, nextGrade);
    }

    public void CellTower(GameObject cellTower)
    {
        EnsurePlayerService();
        Tower towerScript = cellTower.GetComponent<Tower>();
        TowerWeapon cellTowerWeapon = cellTower.GetComponent<TowerWeapon>();
        if (towerScript == null || cellTowerWeapon == null)
        {
            return;
        }

        int refund = TowerSellPricing.CalculateRefund(
            cellTowerWeapon.towerGrade,
            cellTowerWeapon.useGoldToUpGrade);

        sessionStats?.RecordTowerSold(cellTowerWeapon.weaponType);
        playerService.AddGold(refund);
        towerScript.DestoryThisTower();
    }

    public void DestoryTower(GameObject tower)
    {
        towerList.Remove(tower);
        Destroy(tower);
    }
}