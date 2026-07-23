using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightning : MonoBehaviour
{
    private int segments = 5;
    private int chainCount = 3;
    private float currentTime;
    private float damage = 1;
    private float searchRadius = 3.0f;
    private float lightningMoveTime = 0.2f;

    GameObject startObject;
    GameObject targetObject;

    Vector2 startPosition;
    Vector2 targetPosition;

    List<GameObject> hitList;

    [SerializeField]
    private LayerMask layerMask;
    [SerializeField]
    private LineRenderer lineRenderer;

    //bool isLightning = false;
    public void SetUp(GameObject Enemy, float damage)
    {
        this.damage = damage;
        startObject = this.gameObject;
        targetObject = Enemy;
    }
    // Start is called before the first frame update
    void Start()
    {
        hitList = new List<GameObject>();
    }

    public void ChainLightningStart()
    {
        lineRenderer.enabled = false;

        hitList.Clear();
        startObject = this.gameObject;
        //startObject = null;

        lineRenderer.enabled = true;
        StartCoroutine("ChainLightningSystem");
    }

    public IEnumerator ChainLightningSystem()
    {
        for (int i = 0; i < chainCount; i++)
        {
            //타겟 오브젝트를 피격 리스트에 넣음
            if (targetObject != null && !hitList.Contains(targetObject))
            {
                hitList.Add(targetObject);
            }

            StartCoroutine("LightningEffect");

            startObject = targetObject; // 이전 타겟을 새로운 시작 오브젝트(기준)로 설정
            targetObject = SearchEnemy(targetObject); // 새로운 타겟을 전 타겟 기준으로 찾음

            //새로운 타겟을 못찾으면 공격 종료
            if (targetObject == null)
            {
                break;
            }
            yield return new WaitForSeconds(lightningMoveTime);
        }

        //피격된 적 리스트에 있는 적들 피격 처리
        foreach (GameObject enemy in hitList)
        {
            if (enemy != null)
            {
                enemy.GetComponent<EnemyHp>().TakeDamage(damage);
            }
        }

        lineRenderer.enabled = false;
    }

    public GameObject SearchEnemy(GameObject standardObject)
    {
        GameObject TargetEnemy = null;
        Collider2D[] colliders = null;

        if (standardObject != null)
        {
             colliders = Physics2D.OverlapCircleAll(standardObject.transform.position, searchRadius, layerMask);
        }
        else //서치 도중 기준 오브젝트가 파괴됌 => null 리턴
        {
            return TargetEnemy;
        }

        float closestDistance = Mathf.Infinity;

        //가장 가까운 충돌체 찾음
        foreach (Collider2D collider in colliders)
        {
            //검출된 충돌체가 파괴되어서 null 이거나 서치된 충돌체가 자기밖에 없거나 이미 맞은 적이면 다음 충돌체 검사
            if (collider == null || standardObject == collider.gameObject
                || hitList.Contains(collider.gameObject))
            {
                continue;
            }
            
            float distance = Vector2.Distance(standardObject.transform.position, 
                collider.gameObject.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                TargetEnemy = collider.gameObject;
            }
        }

        return TargetEnemy;
    }

    //시작과 타겟에 번개 이펙트를 여러변 그려주는 코루틴
    public IEnumerator LightningEffect()
    {
        if (startObject == null || targetObject == null) 
        {
            yield break;
        }

        lineRenderer.enabled = true;

        startPosition = startObject.transform.position;
        targetPosition = targetObject.transform.position;

        currentTime = Time.time;
        while (true)
        {
            if (Time.time - currentTime > lightningMoveTime)
            {
                break;
            }
            DrawLightning(startPosition, targetPosition, segments);
            yield return new WaitForEndOfFrame();
        }
    }

    //번개 이펙트 , 선이 랜덤으로 지그재그 형태를 띤다.
    public void DrawLightning(Vector2 source, Vector2 target, int segments)
    {
        lineRenderer.positionCount = segments;
        lineRenderer.SetPosition(0, source);

        for (int i = 1; i < segments - 1; i++)
        {
            Vector2 pos = Vector2.Lerp(source, target, (float)i / (float)segments);
            pos += Random.insideUnitCircle * 0.1f;
            lineRenderer.SetPosition(i, pos);
        }
        lineRenderer.SetPosition(segments - 1, target);
    }
}
