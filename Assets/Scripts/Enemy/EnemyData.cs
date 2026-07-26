using UnityEngine;

[CreateAssetMenu(menuName = "RPD/Enemy Data", fileName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    [Tooltip("밸런스/웨이브 키. 비우면 asset 이름")]
    public string enemyId;

    [Tooltip("스테이지 웨이브 드롭다운과 매칭. Catalog 조회 키")]
    public EnemyType enemyType;

    [Tooltip("UI·로그용. 비우면 Id")]
    public string displayName;

    public int gold = 1;
    public int scorePoint = 10;
    public float maxHp = 10f;
    public float moveSpeed = 2f;
    public float rotateSpeed = 0f;

    public Sprite sprite;

    [Tooltip("같은 스프라이트도 색으로 구분")]
    public Color spriteColor = Color.white;

    public string Id => string.IsNullOrEmpty(enemyId) ? name : enemyId;

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? Id : displayName;
}
