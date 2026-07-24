using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TowerSpawner : MonoBehaviour
{
    public bool testTowerOn;

    [SerializeField]
    private EnemySpawner enemySpawner;

    [SerializeField]
    private GameObject TestTower;

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

    void Awake()
    {
        towerList = new List<GameObject>();
        EnsureCatalog();
        EnsureBaseLoader();
    }

    void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
        EnsureCatalog();
        EnsureBaseLoader();
        TryApplyBalance();
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
        StartCoroutine(SpawnTowerRoutine(towerLocation, grade));
    }

    private IEnumerator SpawnTowerRoutine(Vector2 towerLocation, TowerGrade grade)
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

        GameObject spawnTower;
        TowerData picked = null;

        if (testTowerOn)
        {
            if (TestTower == null)
            {
                Debug.LogError("[TowerSpawner] TestTower is not set.");
                yield break;
            }

            spawnTower = Instantiate(TestTower, tileCenterPosition, Quaternion.identity);
        }
        else
        {
            EnsureCatalog();
            EnsureBaseLoader();
            if (catalog == null)
            {
                yield break;
            }

            picked = catalog.PickRandom(grade);
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
                Debug.LogError($"[TowerSpawner] grade {(int)grade} 프리팹 없음. Library/Addressables 확인.");
                yield break;
            }

            spawnTower = Instantiate(prefab, tileCenterPosition, Quaternion.identity);
        }

        TowerWeapon spawnTowerWeapon = spawnTower.GetComponent<TowerWeapon>();
        Tower spawnTowerScript = spawnTower.GetComponent<Tower>();
        if (spawnTowerWeapon == null || spawnTowerScript == null)
        {
            Debug.LogError($"[TowerSpawner] Prefab '{spawnTower.name}' missing TowerWeapon/Tower.");
            Destroy(spawnTower);
            yield break;
        }

        if (enemySpawner == null)
        {
            Debug.LogError("[TowerSpawner] EnemySpawner not assigned.");
            Destroy(spawnTower);
            yield break;
        }

        if (picked != null)
        {
            spawnTowerWeapon.BindDefinition(picked);
        }

        towerList.Add(spawnTower);
        spawnTowerWeapon.SetUp(this, enemySpawner);
        spawnTowerScript.SetUp(this, MapDirector.Instance.aStarGrid.GetNodeFromWorld(tileCenterPosition));
    }

    public void CombineTower(GameObject tower)
    {
        TowerWeapon selectTowerWeapon = tower.GetComponent<TowerWeapon>();
        if (selectTowerWeapon == null)
        {
            return;
        }

        TowerGrade materialGrade = selectTowerWeapon.towerGrade;
        if ((int)materialGrade >= Constants.MaxTowerGrade)
        {
            Debug.Log($"[TowerSpawner] Already max grade {Constants.MaxTowerGrade}.");
            return;
        }

        List<GameObject> sameTower = new List<GameObject>();
        for (int i = 0; i < towerList.Count; i++)
        {
            TowerWeapon other = towerList[i].GetComponent<TowerWeapon>();
            if (other == null)
            {
                continue;
            }

            if (other.weaponType != selectTowerWeapon.weaponType)
            {
                continue;
            }

            if (other.towerGrade != materialGrade)
            {
                continue;
            }

            sameTower.Add(towerList[i]);
            if (sameTower.Count != Constants.towerCombineCount)
            {
                continue;
            }

            Vector3 spawnPos = sameTower[Random.Range(0, Constants.towerCombineCount)].transform.position;

            foreach (GameObject materialTower in sameTower)
            {
                materialTower.GetComponent<Tower>().DestoryThisTower();
            }

            TowerGrade nextGrade = materialGrade + 1;
            EnsureCatalog();
            if (catalog == null || !catalog.HasAny(nextGrade))
            {
                Debug.LogError($"[TowerSpawner] No towers for grade {(int)nextGrade}.");
                return;
            }

            SpawnTower(spawnPos, nextGrade);
            break;
        }
    }

    public void CellTower(GameObject cellTower)
    {
        Tower towerScript = cellTower.GetComponent<Tower>();
        TowerWeapon cellTowerWeapon = cellTower.GetComponent<TowerWeapon>();

        int cellGold = Constants.spawnRandomTowerGold;
        for (int i = 1; i < (int)cellTowerWeapon.towerGrade; i++)
        {
            cellGold *= Constants.towerCombineCount;
        }

        cellGold += cellTowerWeapon.useGoldToUpGrade;
        cellGold = (int)(cellGold * Constants.cellTowerReturnGoldMulti);

        playerService.AddGold(cellGold);
        towerScript.DestoryThisTower();
    }

    public void DestoryTower(GameObject tower)
    {
        towerList.Remove(tower);
        Destroy(tower);
    }
}