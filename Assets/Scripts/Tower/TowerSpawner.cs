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

    private TowerCatalog catalog;
    private List<GameObject> towerList;
    private IPlayerService playerService;
    private ITowerBalanceSource balanceSource;

    void Awake()
    {
        towerList = new List<GameObject>();
        EnsureCatalog();
    }

    void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
        EnsureCatalog();
        TryApplyBalance();
    }

    private void EnsureCatalog()
    {
        if (catalog != null)
        {
            return;
        }

        catalog = TowerCatalog.LoadFromResources();
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
        // CSV 없어도 SO 값으로 플레이 가능
    }

    public void SpawnTower(Vector2 towerSpawnPosition)
    {
        SpawnTower(towerSpawnPosition, (TowerGrade)Constants.ShopSpawnGrade);
    }

    public void SpawnTower(Vector2 towerLocation, TowerGrade grade)
    {
        if (towerList == null)
        {
            towerList = new List<GameObject>();
        }

        if (MapDirector.Instance == null || MapDirector.Instance.aStarGrid == null)
        {
            Debug.LogError("[TowerSpawner] MapDirector not ready.");
            return;
        }

        AStarNode wallNode = MapDirector.Instance.aStarGrid.GetNodeFromWorld(towerLocation);
        if (wallNode != null && wallNode.isBuildTower)
        {
            return;
        }

        if (WallMap == null)
        {
            Debug.LogError("[TowerSpawner] WallMap not assigned.");
            return;
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
                return;
            }

            spawnTower = Instantiate(TestTower, tileCenterPosition, Quaternion.identity);
        }
        else
        {
            EnsureCatalog();
            if (catalog == null)
            {
                return;
            }

            picked = catalog.PickRandom(grade);
            GameObject prefab = catalog.ResolvePrefab(picked);
            if (picked == null || prefab == null)
            {
                Debug.LogError($"[TowerSpawner] grade {(int)grade} 풀이 비었음. Resources/TowerData 의 grade 확인.");
                return;
            }

            spawnTower = Instantiate(prefab, tileCenterPosition, Quaternion.identity);
        }

        TowerWeapon spawnTowerWeapon = spawnTower.GetComponent<TowerWeapon>();
        Tower spawnTowerScript = spawnTower.GetComponent<Tower>();
        if (spawnTowerWeapon == null || spawnTowerScript == null)
        {
            Debug.LogError($"[TowerSpawner] Prefab '{spawnTower.name}' missing TowerWeapon/Tower.");
            Destroy(spawnTower);
            return;
        }

        if (enemySpawner == null)
        {
            Debug.LogError("[TowerSpawner] EnemySpawner not assigned.");
            Destroy(spawnTower);
            return;
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
                Debug.LogError($"[TowerSpawner] No towers for grade {(int)nextGrade}. Resources/TowerData 확인.");
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