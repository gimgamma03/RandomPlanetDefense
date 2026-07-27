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

    [Tooltip("전투 역할. 스탯 변형은 SO 복제+색으로")]
    public EnemyRole enemyRole = EnemyRole.Swarm;

    [Tooltip("강도 티어 1~3. 웨이브에서 Type+Tier로 고른다")]
    public EnemyTier enemyTier = EnemyTier.Tier1;

    public int gold = 1;
    public int scorePoint = 10;
    public float maxHp = 10f;
    public float moveSpeed = 2f;
    public float rotateSpeed = 0f;

    [Min(0.1f)]
    [Tooltip("표시·콜라이더 기준 스케일. 분열 잔해는 보통 0.6~0.8")]
    public float visualScale = 1f;

    [Tooltip("Shielded: 본체 HP 전에 깎이는 실드량. 0이면 역할만 표시")]
    public float shieldHp = 0f;

    [Min(0)]
    [Tooltip("Splitter: 사망 시 스폰 수. 0이면 분열 안 함")]
    public int splitCount = 2;

    [Tooltip("Splitter: 분열체 EnemyType (보통 Swarm)")]
    public EnemyType splitChildType = EnemyType.Swarm;

    public Sprite sprite;

    [Tooltip("같은 스프라이트도 색으로 구분 (엘리트 러너 등)")]
    public Color spriteColor = Color.white;

    [Header("Boss")]
    [Tooltip("보스 실루엣 네온 + 왕관 마커")]
    public bool isBoss;

    [Tooltip("보스 주위를 도는 보조 왕관 스프라이트 (노란 네온 등)")]
    public Sprite bossCrownSprite;

    public string Id => string.IsNullOrEmpty(enemyId) ? name : enemyId;

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? Id : displayName;

    public bool HasShield => enemyRole == EnemyRole.Shielded && shieldHp > 0f;

    public bool CanSplit => enemyRole == EnemyRole.Splitter && splitCount > 0;
}
