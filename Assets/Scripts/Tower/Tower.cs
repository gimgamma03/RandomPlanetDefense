using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [SerializeField]
    private float RotateSpeed = 0.1f;

    public AStarNode towerNode;
    private TowerSpawner towerSpawner;

    public void SetUp(TowerSpawner towerSpawner, AStarNode towerNode)
    {
        this.towerSpawner = towerSpawner;
        this.towerNode = towerNode;
        this.towerNode.isBuildTower = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.forward * RotateSpeed);
    }

    public void DestoryThisTower()
    {
        towerNode.isBuildTower = false;
        towerSpawner.DestoryTower(this.gameObject);
        //Destroy(this.gameObject);
    }
}
