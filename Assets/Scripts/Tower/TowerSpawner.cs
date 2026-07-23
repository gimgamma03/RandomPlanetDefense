using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TowerGrade
{
    Grade1 = 1,
    Grade2,
    Grade3,
    Grade4,
};
public class TowerSpawner : MonoBehaviour
{
    public bool testTowerOn;

    [SerializeField]
    private Player player;

    [SerializeField]
    private TowerData[] towerData;

    [SerializeField]
    private EnemySpawner enemySpawner;

    [SerializeField]
    private GameObject[] towerGrade1Prefabs;
    [SerializeField]
    private GameObject[] towerGrade2Prefabs;
    [SerializeField]
    private GameObject[] towerGrade3Prefabs;
    [SerializeField]
    private GameObject[] towerGrade4Prefabs;

    [SerializeField]
    private GameObject TestTower;

    [SerializeField]
    private Tilemap WallMap;

    public List<GameObject[]> towerTypeList;
    //private float towerCellGoldPercent = 0.8f;
    private List<GameObject> towerList;
    private int towerCombineCount = 3;

    // Start is called before the first frame update
    void Start()
    {
        towerList = new List<GameObject>();
        towerTypeList = new List<GameObject[]>
        {
            towerGrade1Prefabs,
            towerGrade2Prefabs,
            towerGrade3Prefabs,
            towerGrade4Prefabs
        };

        //Debug.Log(towerTypeList[2].Length);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SpawnTower(Vector2 towerSpawnPosition)
    {
        AStarNode WallNode = MapDirector.Instance.aStarGrid.GetNodeFromWorld(towerSpawnPosition);

        if (WallNode.isBuildTower)
        {
            return;
        }

        SpawnTower(towerSpawnPosition, TowerGrade.Grade1);
        return;

        /*Vector3Int tilePosition = MapDirector.Instance.WallMap.WorldToCell(towerSpawnPosition);

        //이 함수를 부르는 PanelGameManager의 함수는 CompareTag로 WallMap 인지 확인을 했기 때문에 hastile 로 체크하던거 없앰

        GameObject Tower;
        if (testTowerOn)
        {
            Tower = Instantiate(TestTower, transform.position, Quaternion.identity);
        }
        else
        {
            Tower = Instantiate(towerGrade1Prefabs[Random.Range(0, towerGrade1Prefabs.Length)], transform.position, Quaternion.identity);
        }

        towerList.Add(Tower);
        TowerWeapon towerWeapon = Tower.GetComponent<TowerWeapon>();
        Tower tower = Tower.GetComponent<Tower>();
        towerWeapon.SetUp(this, enemySpawner);
        tower.SetUp(this, WallNode);

        Vector3 CenterPosition = WallMap.GetCellCenterWorld(tilePosition);
        CenterPosition -= WallMap.cellGap / 2;

        Tower.transform.position = CenterPosition;*/
    }
    public void SpawnTower(Vector2 towerLocation, TowerGrade grade)
    {
        AStarNode WallNode = MapDirector.Instance.aStarGrid.GetNodeFromWorld(towerLocation);

        if (WallNode.isBuildTower)
        {
            return;
        }

        int gradeIndex = (int)(grade - 1);
        Vector3Int tilePosition = MapDirector.Instance.WallMap.WorldToCell(towerLocation);

        Vector3 tileCenterPosition = MapDirector.Instance.WallMap.GetCellCenterWorld(tilePosition);
        tileCenterPosition -= WallMap.cellGap / 2;

        GameObject spawnTower;
        if (testTowerOn)
        {
            //테스트 할 타워 생성
            spawnTower = Instantiate(TestTower, tileCenterPosition, Quaternion.identity);
        }
        else
        {
            //지정된 등급의 타워중에서 랜덤으로 소환, 위치는 타일의 중앙
            spawnTower = Instantiate(towerTypeList[gradeIndex][Random.Range(0, towerTypeList[gradeIndex].Length)],
                tileCenterPosition, Quaternion.identity);
        }

        towerList.Add(spawnTower);
        TowerWeapon spawntowerWeapon = spawnTower.GetComponent<TowerWeapon>();
        Tower spawnTowerScript = spawnTower.GetComponent<Tower>();
        spawntowerWeapon.SetUp(this, enemySpawner);
        spawnTowerScript.SetUp(this, MapDirector.Instance.aStarGrid.GetNodeFromWorld(tileCenterPosition));
    }
    
    public void CombineTower(GameObject Tower)
    {
        TowerWeapon selectTowerWeapon = Tower.GetComponent<TowerWeapon>();
        Tower selectTowerScript = Tower.GetComponent<Tower>();
        TowerGrade materialTowerGrade = selectTowerWeapon.towerGrade;
        WeaponType towerWeaponType = selectTowerWeapon.weaponType;

        Debug.Log("before combine tower Count : " + towerList.Count);

        int[] findSameTower = new int[towerCombineCount];
        List<GameObject> SameTower = new List<GameObject>();
        int SearchCount = 0;

        for (int i = 0; i < towerList.Count; i++)
        {
            TowerWeapon towerWeaponInList = towerList[i].GetComponent<TowerWeapon>();
            if (towerWeaponInList.weaponType == selectTowerWeapon.weaponType)
            {
                SameTower.Add(towerList[i]);
                findSameTower[SearchCount] = i;
                SearchCount++;

                if (SearchCount == Constants.towerCombineCount)
                {
                    //upGradeTowerNode = SameTower[Random.Range(0, towerCombineCount)].GetComponent<Tower>().towerNode;

                    //List 에서 하나씩 인덱스로 삭제 했더니 
                    //삭제 후 리스트 가 알아서 재정비해서 인덱스가 안맞아서 한번에 삭제하는식으로 함
                    foreach (GameObject materialTower in SameTower)
                    {
                        //3개의 현재 등급의 재료 타워 삭제
                        Tower towerScript = materialTower.GetComponent<Tower>();
                        towerScript.DestoryThisTower();
                    }

                    //다음 등급의 타워 생성
                    SpawnTower(SameTower[Random.Range(0, towerCombineCount)].transform.position, materialTowerGrade + 1);
                    break;
                }
            }
        }

    }

    public void CellTower(GameObject cellTower)
    {
        Tower towerScript = cellTower.GetComponent<Tower>();
        TowerWeapon cellTowerWeapon = cellTower.GetComponent<TowerWeapon>();

        int cellGold = Constants.spawnRandomTowerGold;

        //등급에 따라 기본적인 타워에 소모된 골드 계산
        for (int i = 1; i < (int)cellTowerWeapon.towerGrade; i++)
        {
            cellGold *= Constants.towerCombineCount;
        }

        //업그레이드에 사용한 골드 계산
        cellGold += cellTowerWeapon.useGoldToUpGrade;

        //비율에 따라 판매 골드 계산
        float cellGoldCal = cellGold * Constants.cellTowerReturnGoldMulti;
        cellGold = (int)cellGoldCal;

        player.gold += cellGold;
        towerScript.DestoryThisTower();
    }
    public void DestoryTower(GameObject Tower)
    {
        towerList.Remove(Tower);
        Destroy(Tower.gameObject);
    }
}
