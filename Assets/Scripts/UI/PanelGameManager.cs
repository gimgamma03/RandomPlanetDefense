using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PanelGameManager : MonoBehaviour
{
    [SerializeField]
    private TowerSpawner towerSpawner;
    [SerializeField]
    private EnemySpawner enemySpawner;

    [SerializeField]
    private GameObject randomTowerSpawnerImage;

    private IPlayerService playerService;
    private bool isTowerCellMode = false;
    private bool isTowerSpawnMode = false;
    private bool isTowerCombineMode = false;
    private bool activeSomeThingButton = false;
    private RaycastHit2D hit;

    //for MouseFollowObject Image color change
    private Renderer panelGameSystemMouseImageRenderer;
    private Material panelMaterial;

    void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
        panelGameSystemMouseImageRenderer = randomTowerSpawnerImage.gameObject.GetComponent<Renderer>();
        panelMaterial = panelGameSystemMouseImageRenderer.material;
        randomTowerSpawnerImage.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelSomethingButton();
        }

        if (activeSomeThingButton)
        {
            randomTowerSpawnerImage.transform.position = MousePosition;
        }

        if (Input.GetMouseButtonDown(0) && activeSomeThingButton)
        {
            // NonRayLayer는 레이캐스트에서 제외
            int layerMask = ~(1 << LayerMask.NameToLayer("NonRayLayer"));
            hit = Physics2D.Raycast(MousePosition, Vector2.zero, Mathf.Infinity, layerMask);

            // 1등급 랜덤 타워 스폰
            if (isTowerSpawnMode)
            {
                if (!playerService.TrySpendGold(Constants.spawnRandomTowerGold))
                {
                    return;
                }

                if (hit.transform == null || !hit.transform.CompareTag("WallMap"))
                {
                    playerService.AddGold(Constants.spawnRandomTowerGold);
                    return;
                }

                towerSpawner.SpawnTower(MousePosition, (TowerGrade)Constants.ShopSpawnGrade);
            }
            else if (isTowerCombineMode)
            {
                if (hit.transform == null || !hit.transform.CompareTag("Tower"))
                {
                    return;
                }

                towerSpawner.CombineTower(hit.transform.gameObject);
            }
            else if (isTowerCellMode)
            {
                if (hit.transform == null || !hit.transform.CompareTag("Tower"))
                {
                    return;
                }

                towerSpawner.CellTower(hit.transform.gameObject);
            }
        }

    }

    private void CancelSomethingButton()
    {
        isTowerSpawnMode = false;
        isTowerCombineMode = false;
        activeSomeThingButton = false;
        randomTowerSpawnerImage.SetActive(false);
    }
    public void RandomTowerSpawnerButton()
    {
        CancelSomethingButton();

        //Debug.Log("enable image");
        isTowerSpawnMode = true;
        activeSomeThingButton = true;
        randomTowerSpawnerImage.gameObject.SetActive(true);
        panelMaterial.color = Color.green;
    }
    public void TowerCombinationButton()
    {
        CancelSomethingButton();

        isTowerCombineMode = true;
        activeSomeThingButton = true;
        randomTowerSpawnerImage.gameObject.SetActive(true);
        panelMaterial.color = Color.blue;

    }
    public void TowerCellButton()
    {
        CancelSomethingButton();

        isTowerCellMode = true;
        activeSomeThingButton = true;
        randomTowerSpawnerImage.gameObject.SetActive(true);
        panelMaterial.color = Color.red;
    }
}